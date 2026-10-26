/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Engine/Yaml.h, src/Engine/Yaml.cpp
//
// The C++ code wraps rapidyaml. This port parses with YamlDotNet's event parser into an own node tree,
// so rapidyaml's behaviour is kept where rulesets and mods rely on it:
// - duplicate map keys are allowed, lookups return the first match,
// - anchors/aliases are copied, merge keys (<<) follow rapidyaml's Tree::resolve(),
// - a stream with one document is that document, several documents form a sequence,
// - scalars are converted like c4core (bools: 0/1/true/false/True/..., ints: 0x/0b/0o prefixes).

using System.Globalization;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Acom;

/// <summary>Error while reading YAML (C++ YAML::Exception).</summary>
public sealed class YamlReadException : Exception
{
	public YamlReadException()
	{
	}

	public YamlReadException(string message) : base(message)
	{
	}

	public YamlReadException(string message, Exception inner) : base(message, inner)
	{
	}
}

internal enum YamlKind
{
	Scalar,
	Map,
	Seq,
}

/// <summary>Immutable-after-parse node of the YAML tree.</summary>
internal sealed class YamlTreeNode
{
	public YamlKind Kind { get; init; }
	public string? Key { get; set; }
	public string Value { get; init; } = "";
	public ScalarStyle Style { get; init; } = ScalarStyle.Plain;
	public string? Tag { get; init; }
	public List<YamlTreeNode> Children { get; } = [];
	public int Line { get; init; }
	public int Column { get; init; }
	public bool IsStream { get; init; }
	public string File { get; init; } = "";

	/// <summary>Plain ~, null or empty value (rapidyaml val_is_null).</summary>
	public bool IsNull => Kind == YamlKind.Scalar && Style == ScalarStyle.Plain && Value is "" or "~" or "null" or "Null" or "NULL";

	public YamlTreeNode Clone(string? key)
	{
		var copy = new YamlTreeNode { Kind = Kind, Key = key, Value = Value, Style = Style, Tag = Tag, Line = Line, Column = Column, File = File };
		foreach (var child in Children)
			copy.Children.Add(child.Clone(child.Key));
		return copy;
	}
}

/// <summary>Builds the node tree from YamlDotNet parser events.</summary>
internal static class YamlTreeBuilder
{
	public static YamlTreeNode? Parse(string yaml, string fileName)
	{
		if (yaml.Length > 0 && yaml[0] == '﻿') // skip UTF-8 BOM
			yaml = yaml[1..];
		var parser = new Parser(new StringReader(yaml));
		var anchors = new Dictionary<string, YamlTreeNode>(StringComparer.Ordinal);
		var docs = new List<YamlTreeNode>();
		try
		{
			parser.Consume<StreamStart>();
			while (!parser.Accept<StreamEnd>(out _))
			{
				parser.Consume<DocumentStart>();
				if (!parser.Accept<DocumentEnd>(out _))
					docs.Add(ParseNode(parser, anchors, null, fileName));
				parser.Consume<DocumentEnd>();
			}
		}
		catch (YamlException e)
		{
			throw new YamlReadException($"{fileName}:{e.Start.Line}:{e.Start.Column}: {e.Message}", e);
		}
		if (docs.Count == 0)
			return null;
		if (docs.Count == 1)
			return docs[0];
		var stream = new YamlTreeNode { Kind = YamlKind.Seq, IsStream = true, Line = 1, Column = 1, File = fileName };
		stream.Children.AddRange(docs);
		return stream;
	}

	private static YamlTreeNode ParseNode(IParser parser, Dictionary<string, YamlTreeNode> anchors, string? key, string file)
	{
		var current = parser.Current ?? throw new YamlReadException($"{file}: unexpected end of YAML");
		int line = (int)current.Start.Line, column = (int)current.Start.Column;
		switch (current)
		{
			case Scalar scalar:
			{
				parser.MoveNext();
				var node = new YamlTreeNode { Kind = YamlKind.Scalar, Key = key, Value = scalar.Value, Style = scalar.Style, Tag = TagOf(scalar.Tag), Line = line, Column = column, File = file };
				Register(anchors, scalar.Anchor, node);
				return node;
			}
			case AnchorAlias alias:
			{
				parser.MoveNext();
				if (!anchors.TryGetValue(alias.Value.Value, out var target))
					throw new YamlReadException($"{file}:{line}:{column}: unknown anchor '{alias.Value.Value}'");
				return target.Clone(key);
			}
			case SequenceStart seqStart:
			{
				parser.MoveNext();
				var node = new YamlTreeNode { Kind = YamlKind.Seq, Key = key, Tag = TagOf(seqStart.Tag), Line = line, Column = column, File = file };
				while (!parser.Accept<SequenceEnd>(out _))
					node.Children.Add(ParseNode(parser, anchors, null, file));
				parser.Consume<SequenceEnd>();
				Register(anchors, seqStart.Anchor, node);
				return node;
			}
			case MappingStart mapStart:
			{
				parser.MoveNext();
				var node = new YamlTreeNode { Kind = YamlKind.Map, Key = key, Tag = TagOf(mapStart.Tag), Line = line, Column = column, File = file };
				var items = new List<YamlTreeNode>();
				bool hasMerge = false;
				while (!parser.Accept<MappingEnd>(out _))
				{
					var keyNode = ParseNode(parser, anchors, null, file);
					if (keyNode.Kind != YamlKind.Scalar)
						throw new YamlReadException($"{file}:{keyNode.Line}:{keyNode.Column}: only scalar keys are supported");
					var value = ParseNode(parser, anchors, keyNode.Value, file);
					hasMerge |= keyNode.Value == "<<" && keyNode.Style == ScalarStyle.Plain;
					items.Add(value);
				}
				parser.Consume<MappingEnd>();
				node.Children.AddRange(hasMerge ? ResolveMerges(items) : items);
				Register(anchors, mapStart.Anchor, node);
				return node;
			}
			default:
				throw new YamlReadException($"{file}:{line}:{column}: unexpected YAML event {current.GetType().Name}");
		}
	}

	/// <summary>
	/// Applies merge keys like rapidyaml's Tree::resolve(): merged keys replace earlier keys,
	/// later explicit keys replace merged ones, the first merge source wins among sources.
	/// </summary>
	private static List<YamlTreeNode> ResolveMerges(List<YamlTreeNode> items)
	{
		var result = new List<YamlTreeNode>();
		var merged = new HashSet<YamlTreeNode>();
		foreach (var item in items)
		{
			if (item.Key == "<<" && item.Kind != YamlKind.Scalar)
			{
				var sources = item.Kind == YamlKind.Seq ? item.Children : [item];
				var inserted = new HashSet<string>(StringComparer.Ordinal);
				foreach (var source in sources.Where(s => s.Kind == YamlKind.Map))
				{
					foreach (var child in source.Children)
					{
						if (!inserted.Add(child.Key!))
							continue;
						result.RemoveAll(r => r.Key == child.Key);
						var copy = child.Clone(child.Key);
						merged.Add(copy);
						result.Add(copy);
					}
				}
				continue;
			}
			int existing = result.FindIndex(r => r.Key == item.Key && merged.Contains(r));
			if (existing >= 0)
			{
				merged.Remove(result[existing]);
				result[existing] = item;
			}
			else
			{
				result.Add(item);
			}
		}
		return result;
	}

	private static string? TagOf(TagName tag) => tag.IsEmpty ? null : tag.Value;

	private static void Register(Dictionary<string, YamlTreeNode> anchors, AnchorName anchor, YamlTreeNode node)
	{
		if (!anchor.IsEmpty)
			anchors[anchor.Value] = node;
	}
}

/// <summary>Root of a parsed YAML document (C++ YamlRootNodeReader).</summary>
public sealed class YamlRootNodeReader
{
	private readonly YamlNodeReader _root;

	/// <summary>Parses YAML text.</summary>
	/// <param name="yaml">YAML text.</param>
	/// <param name="description">File name or description used in error messages.</param>
	public YamlRootNodeReader(string yaml, string description)
	{
		ArgumentNullException.ThrowIfNull(yaml);
		ArgumentNullException.ThrowIfNull(description);
		// find only name of file, not whole path
		string name = description[(description.LastIndexOfAny(['/', '\\']) + 1)..];
		_root = new YamlNodeReader(YamlTreeBuilder.Parse(yaml, name));
	}

	/// <summary>Parses UTF-8 YAML data.</summary>
	public YamlRootNodeReader(ReadOnlySpan<byte> data, string fileNameForError) : this(Encoding.UTF8.GetString(data), fileNameForError)
	{
	}

	/// <summary>Reads a YAML file (C++ <c>YamlRootNodeReader(fullFilePath, onlyInfoHeader)</c>).</summary>
	/// <param name="fullFilePath">File to read.</param>
	/// <param name="onlyInfoHeader">Read only the first document of a savegame (up to the first "\n---"), for the load/save lists.</param>
	public static YamlRootNodeReader FromFile(string fullFilePath, bool onlyInfoHeader = false)
	{
		byte[] data = onlyInfoHeader ? CrossPlatform.GetYamlSaveHeaderRaw(fullFilePath) : CrossPlatform.ReadFileRaw(fullFilePath);
		int length = data.Length;
		if (onlyInfoHeader)
		{
			// C++: str.find("\n---") + 1, i.e. npos + 1 == 0 (empty document) without a separator
			length = data.AsSpan().IndexOf("\n---"u8) + 1;
		}
		return new YamlRootNodeReader(data.AsSpan(0, length), fullFilePath);
	}

	/// <summary>The root node.</summary>
	public YamlNodeReader ToBase() => _root;

	public YamlNodeReader this[string key] => _root[key];

	public YamlNodeReader this[int pos] => _root[pos];

	public YamlNodeReader UseIndex() => _root.UseIndex();

	public bool IsValid => _root.IsValid;
}

/// <summary>
/// Read access to one YAML node (C++ YamlNodeReader). Invalid readers are returned for missing
/// nodes, so lookups can be chained: <c>reader["a"]["b"].IsValid</c>.
/// </summary>
public readonly struct YamlNodeReader
{
	private readonly YamlTreeNode? _node;
	private readonly Dictionary<string, YamlTreeNode>? _index;

	internal YamlNodeReader(YamlTreeNode? node, Dictionary<string, YamlTreeNode>? index = null)
	{
		_node = node;
		_index = index;
	}

	internal YamlTreeNode? Node => _node;

	/// <summary>Is this an existing node?</summary>
	public bool IsValid => _node != null;

	public bool IsMap => _node?.Kind == YamlKind.Map;

	public bool IsSeq => _node?.Kind == YamlKind.Seq;

	/// <summary>Is this a scalar (including an empty value)?</summary>
	public bool HasVal => _node?.Kind == YamlKind.Scalar;

	public bool HasNullVal => _node is { IsNull: true };

	public bool HasValTag => _node?.Tag != null;

	/// <summary>Checks the node's tag, e.g. <c>"!add"</c>.</summary>
	public bool HasValTagNamed(string tagName) => _node?.Tag == tagName;

	/// <summary>The node's tag or an empty string.</summary>
	public string ValTag => _node?.Tag ?? string.Empty;

	/// <summary>Key of this node in its parent map.</summary>
	public string Key => _node?.Key ?? throw new YamlReadException("Tried getting key of an invalid node!");

	/// <summary>Raw scalar value.</summary>
	public string Val => _node == null ? throw new YamlReadException("Tried getting value of an invalid node!") : _node.Value;

	/// <summary>1-based line and column in the source file.</summary>
	public (int Line, int Column) LocationInFile => _node == null ? (0, 0) : (_node.Line, _node.Column);

	/// <summary>
	/// Returns a reader with a key index for fast repeated lookups. An empty value (<c>~</c>) counts as
	/// empty map; sequences and other values are errors.
	/// </summary>
	public YamlNodeReader UseIndex()
	{
		if (_node == null)
			return this;
		var index = new Dictionary<string, YamlTreeNode>(StringComparer.Ordinal);
		if (_node.Kind != YamlKind.Map)
		{
			if (_node.Kind == YamlKind.Seq)
			{
				if (_node.IsStream)
					this[1].ThrowNodeError("multi-document yaml file with splits '---'");
				ThrowNodeError("sequence node as map");
			}
			if (!_node.IsNull)
				ThrowNodeError("value node as map");
			return new YamlNodeReader(_node, index);
		}
		foreach (var child in _node.Children)
			index.TryAdd(child.Key!, child);
		return new YamlNodeReader(_node, index);
	}

	/// <summary>Number of children (unique keys when indexed).</summary>
	public int ChildrenCount => _node == null ? 0 : _index?.Count ?? _node.Children.Count;

	/// <summary>All children in file order.</summary>
	public IReadOnlyList<YamlNodeReader> Children()
	{
		if (_node == null)
			return [];
		return _node.Children.Select(c => new YamlNodeReader(c)).ToList();
	}

	/// <summary>Child of a map by key (first match), or an invalid reader.</summary>
	public YamlNodeReader this[string key] => new(GetChildNode(key));

	/// <summary>Child by position, or an invalid reader.</summary>
	public YamlNodeReader this[int pos] => _node != null && pos >= 0 && pos < _node.Children.Count ? new YamlNodeReader(_node.Children[pos]) : default;

	private YamlTreeNode? GetChildNode(string key)
	{
		if (_node == null)
			return null;
		if (_index != null)
			return _index.GetValueOrDefault(key);
		if (_node.Kind != YamlKind.Map)
			return null;
		foreach (var child in _node.Children)
		{
			if (child.Key == key)
				return child;
		}
		return null;
	}

	/// <summary>Reads a child value; returns false (and leaves <paramref name="outputValue"/> unchanged) if the key is missing.</summary>
	/// <exception cref="YamlReadException">The child exists but has the wrong type.</exception>
	public bool TryRead<T>(string key, ref T outputValue)
	{
		var child = GetChildNode(key);
		if (child == null)
			return false;
		if (!YamlConvert.TryRead(child, ref outputValue))
			ThrowTypeError(child, typeof(T));
		return true;
	}

	/// <summary>Reads a required child value.</summary>
	public void ReadNode<T>(string key, ref T outputValue)
	{
		if (!TryRead(key, ref outputValue))
			ThrowNodeError($"missing node '{key}'");
	}

	/// <summary>Reads a child value or uses a default.</summary>
	public void ReadNode<T>(string key, ref T outputValue, T defaultValue)
	{
		if (!TryRead(key, ref outputValue))
			outputValue = defaultValue;
	}

	/// <summary>Reads a child value or returns a default.</summary>
	public T Read<T>(string key, T defaultValue)
	{
		T value = defaultValue;
		TryRead(key, ref value);
		return value;
	}

	public bool TryReadKey<T>(ref T outputValue)
	{
		if (_node?.Key == null)
			return false;
		var keyNode = new YamlTreeNode { Kind = YamlKind.Scalar, Value = _node.Key, Line = _node.Line, Column = _node.Column, File = _node.File };
		if (!YamlConvert.TryRead(keyNode, ref outputValue))
			ThrowTypeError(keyNode, typeof(T));
		return true;
	}

	public T ReadKey<T>()
	{
		T output = default!;
		if (!TryReadKey(ref output))
			ThrowNodeError("invalid node's key");
		return output;
	}

	public T ReadKey<T>(T defaultValue)
	{
		T output = default!;
		return TryReadKey(ref output) ? output : defaultValue;
	}

	public bool TryReadVal<T>(ref T outputValue)
	{
		if (_node == null)
			return false;
		if (!YamlConvert.TryRead(_node, ref outputValue))
			ThrowTypeError(_node, typeof(T));
		return true;
	}

	public T ReadVal<T>()
	{
		T output = default!;
		if (!TryReadVal(ref output))
			ThrowNodeError("invalid node's value");
		return output;
	}

	public T ReadVal<T>(T defaultValue)
	{
		T output = default!;
		return TryReadVal(ref output) ? output : defaultValue;
	}

	/// <summary>Serializes this node (and its children) back to YAML text.</summary>
	public string Emit() => _node == null ? string.Empty : YamlEmitter.Emit(YamlEmitter.FromReader(_node));

	/// <summary>Serializes the children of a map/seq node as a new root document; empty for other nodes (C++ emitDescendants).</summary>
	public string EmitDescendants() => EmitDescendants(default);

	/// <summary>
	/// Serializes the children of a map/seq node as a new root document, plus the children of
	/// <paramref name="defaultValuesReader"/> whose keys are still missing (appended at the end).
	/// Empty for a scalar or invalid node (C++ <c>emitDescendants(defaultValuesReader)</c>).
	/// </summary>
	public string EmitDescendants(YamlNodeReader defaultValuesReader)
	{
		YamlWriteNode root;
		if (IsMap)
			root = new YamlWriteNode { Kind = YamlWriteNode.NodeKind.Map };
		else if (IsSeq)
			root = new YamlWriteNode { Kind = YamlWriteNode.NodeKind.Seq };
		else
			return string.Empty;
		foreach (var child in _node!.Children)
			root.Children.Add(YamlEmitter.FromReader(child));
		if (defaultValuesReader._node is { } defaults)
		{
			foreach (var child in defaults.Children)
			{
				if (!root.Children.Exists(c => c.Key == child.Key))
					root.Children.Add(YamlEmitter.FromReader(child));
			}
		}
		return YamlEmitter.Emit(root);
	}

	/// <summary>Decodes a base64 scalar into binary data (C++ readValBase64).</summary>
	public byte[] ReadValBase64() => Convert.FromBase64String(Val);

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	private static void ThrowTypeError(YamlTreeNode node, Type type)
	{
		string what = node.Kind == YamlKind.Scalar ? $"'{node.Value}'" : node.Kind.ToString().ToLowerInvariant();
		throw new YamlReadException($"{node.File}:{node.Line}:{node.Column}: cannot read {what} as {type.Name}" + (node.Key != null ? $" (key '{node.Key}')" : ""));
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal void ThrowNodeError(string what)
	{
		var (line, column) = LocationInFile;
		throw new YamlReadException($"{_node?.File}:{line}:{column}: {what}" + (_node?.Key != null ? $" (key '{_node.Key}')" : ""));
	}

	/// <summary>Same as <see cref="IsValid"/> (C++ operator bool).</summary>
	public static implicit operator bool(YamlNodeReader reader) => reader.IsValid;

	public bool ToBoolean() => IsValid;
}

/// <summary>Conversion between YAML scalars/containers and .NET values, following c4core/rapidyaml rules.</summary>
internal static class YamlConvert
{
	public static bool TryRead<T>(YamlTreeNode node, ref T value)
	{
		object? boxed = value;
		if (!TryRead(typeof(T), node, ref boxed))
			return false;
		value = (T)boxed!;
		return true;
	}

	private static bool TryRead(Type type, YamlTreeNode node, ref object? value)
	{
		if (type == typeof(string))
		{
			if (node.Kind != YamlKind.Scalar)
				return false;
			value = node.Value;
			return true;
		}
		if (Nullable.GetUnderlyingType(type) is { } inner)
		{
			if (node.Kind != YamlKind.Scalar)
				return false;
			if (node.IsNull)
			{
				value = null;
				return true;
			}
			object? v = Activator.CreateInstance(inner);
			if (!TryRead(inner, node, ref v))
				return false;
			value = v;
			return true;
		}
		if (type == typeof(bool))
		{
			if (node.Kind != YamlKind.Scalar || !TryParseBool(node.Value, out bool b))
				return false;
			value = b;
			return true;
		}
		if (type.IsEnum)
		{
			if (node.Kind != YamlKind.Scalar || !TryParseInteger(node.Value, signed: true, out long e, out _))
				return false;
			value = Enum.ToObject(type, e);
			return true;
		}
		if (IsInteger(type))
			return node.Kind == YamlKind.Scalar && TryParseIntegerOf(type, node.Value, ref value);
		if (type == typeof(double) || type == typeof(float))
		{
			if (node.Kind != YamlKind.Scalar || !TryParseFloat(node.Value, out double d))
				return false;
			value = type == typeof(float) ? (object)(float)d : d;
			return true;
		}
		if (type.IsGenericType)
		{
			var def = type.GetGenericTypeDefinition();
			var args = type.GetGenericArguments();
			if (def == typeof(List<>))
			{
				// C++ clears the vector and reads one element per child (a scalar gives an empty vector).
				var list = (System.Collections.IList)Activator.CreateInstance(type)!;
				foreach (var child in node.Children)
				{
					object? element = DefaultOf(args[0]);
					if (!TryRead(args[0], child, ref element))
						return false;
					list.Add(element);
				}
				value = list;
				return true;
			}
			if (def == typeof(Dictionary<,>) || def == typeof(SortedDictionary<,>))
			{
				// C++ clears the map and emplaces each child: string keys sort ordinally (std::less<std::string>),
				// and a duplicate key keeps the first value (emplace does not overwrite).
				var map = (System.Collections.IDictionary)(args[0] == typeof(string)
					? Activator.CreateInstance(type, StringComparer.Ordinal)
					: Activator.CreateInstance(type))!;
				foreach (var child in node.Children)
				{
					if (child.Key == null)
						return false;
					object? k = DefaultOf(args[0]);
					var keyNode = new YamlTreeNode { Kind = YamlKind.Scalar, Value = child.Key };
					object? v = DefaultOf(args[1]);
					if (!TryRead(args[0], keyNode, ref k) || !TryRead(args[1], child, ref v))
						return false;
					if (!map.Contains(k!))
						map.Add(k!, v);
				}
				value = map;
				return true;
			}
			if (type.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal))
			{
				// pairs/tuples are sequences with exactly as many elements
				if (node.Kind != YamlKind.Seq || node.Children.Count != args.Length)
					return false;
				var items = new object?[args.Length];
				for (int i = 0; i < args.Length; ++i)
				{
					items[i] = DefaultOf(args[i]);
					if (!TryRead(args[i], node.Children[i], ref items[i]))
						return false;
				}
				value = Activator.CreateInstance(type, items);
				return true;
			}
		}
		if (type.IsArray && value is Array existing)
		{
			// fixed-size arrays (C++ std::array) need exactly as many elements
			var elementType = type.GetElementType()!;
			if (node.Kind != YamlKind.Seq || node.Children.Count != existing.Length)
				return false;
			var array = Array.CreateInstance(elementType, existing.Length);
			for (int i = 0; i < existing.Length; ++i)
			{
				object? element = DefaultOf(elementType);
				if (!TryRead(elementType, node.Children[i], ref element))
					return false;
				array.SetValue(element, i);
			}
			value = array;
			return true;
		}
		if (CustomReader(type) is { } custom)
		{
			// like the C++ read(ryml::ConstNodeRef, T*) overloads: public static bool YamlRead(YamlNodeReader, ref T)
			var args = new object?[] { new YamlNodeReader(node), value ?? DefaultOf(type) };
			if (!(bool)custom.Invoke(null, args)!)
				return false;
			value = args[1];
			return true;
		}
		throw new NotSupportedException($"YAML conversion to {type} is not supported");
	}

	private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, System.Reflection.MethodInfo?> s_customReaders = new();
	private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, System.Reflection.MethodInfo?> s_customWriters = new();

	private static System.Reflection.MethodInfo? CustomReader(Type type) => s_customReaders.GetOrAdd(type, t =>
		t.GetMethod("YamlRead", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(YamlNodeReader), t.MakeByRefType()]));

	/// <summary>A type's <c>public static void YamlWrite(YamlNodeWriter, T)</c> (C++ write(ryml::NodeRef*, const T&amp;) overloads).</summary>
	internal static System.Reflection.MethodInfo? CustomWriter(Type type) => s_customWriters.GetOrAdd(type, t =>
		t.GetMethod("YamlWrite", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(YamlNodeWriter), t]));

	private static object? DefaultOf(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

	private static bool IsInteger(Type t) =>
		t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
		t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte) || t == typeof(char);

	private static bool TryParseIntegerOf(Type t, string s, ref object? value)
	{
		bool signed = t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(sbyte);
		if (!TryParseInteger(s, signed, out long n, out ulong u))
			return false;
		try
		{
			value = t switch
			{
				_ when t == typeof(int) => checked((int)n),
				_ when t == typeof(long) => n,
				_ when t == typeof(short) => checked((short)n),
				_ when t == typeof(sbyte) => checked((sbyte)n),
				_ when t == typeof(uint) => checked((uint)u),
				_ when t == typeof(ulong) => u,
				_ when t == typeof(ushort) => checked((ushort)u),
				_ when t == typeof(byte) => checked((byte)u),
				_ => (object)checked((char)u),
			};
			return true;
		}
		catch (OverflowException)
		{
			return false;
		}
	}

	/// <summary>Integer parsing like c4::atoi: optional sign, 0x/0b/0o prefixes, no trailing characters.</summary>
	internal static bool TryParseInteger(string s, bool signed, out long n, out ulong u)
	{
		n = 0;
		u = 0;
		var span = s.AsSpan().Trim();
		bool negative = false;
		if (span.Length > 0 && (span[0] == '-' || span[0] == '+'))
		{
			negative = span[0] == '-';
			if (negative && !signed)
				return false;
			span = span[1..];
		}
		int radix = 10;
		if (span.Length > 2 && span[0] == '0')
		{
			radix = char.ToLowerInvariant(span[1]) switch { 'x' => 16, 'b' => 2, 'o' => 8, _ => 10 };
			if (radix != 10)
				span = span[2..];
		}
		if (span.IsEmpty)
			return false;
		ulong acc = 0;
		foreach (char c in span)
		{
			int digit = c switch
			{
				>= '0' and <= '9' => c - '0',
				>= 'a' and <= 'f' => c - 'a' + 10,
				>= 'A' and <= 'F' => c - 'A' + 10,
				_ => 99,
			};
			if (digit >= radix)
				return false;
			try
			{
				acc = checked(acc * (ulong)radix + (ulong)digit);
			}
			catch (OverflowException)
			{
				return false;
			}
		}
		u = acc;
		if (signed)
		{
			if (negative ? acc > (ulong)long.MaxValue + 1 : acc > long.MaxValue)
				return false;
			n = negative ? unchecked(-(long)acc) : (long)acc;
		}
		return true;
	}

	/// <summary>Bool parsing like c4core: 0/1, false/False/FALSE, true/True/TRUE, else any integer.</summary>
	internal static bool TryParseBool(string s, out bool b)
	{
		switch (s)
		{
			case "0" or "false" or "False" or "FALSE":
				b = false;
				return true;
			case "1" or "true" or "True" or "TRUE":
				b = true;
				return true;
		}
		bool ok = TryParseInteger(s, true, out long n, out _);
		b = n != 0;
		return ok;
	}

	internal static bool TryParseFloat(string s, out double d)
	{
		switch (s.Trim())
		{
			case ".inf" or ".Inf" or ".INF" or "+.inf":
				d = double.PositiveInfinity;
				return true;
			case "-.inf" or "-.Inf" or "-.INF":
				d = double.NegativeInfinity;
				return true;
			case ".nan" or ".NaN" or ".NAN":
				d = double.NaN;
				return true;
		}
		return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
	}

	/// <summary>Formats a value as YAML scalar text (bools as true/false, floats round-trippable).</summary>
	internal static string FormatScalar(object value) => value switch
	{
		bool b => b ? "true" : "false",
		float f => f.ToString("R", CultureInfo.InvariantCulture),
		double d => d.ToString("R", CultureInfo.InvariantCulture),
		Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
		IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString() ?? "",
	};
}
