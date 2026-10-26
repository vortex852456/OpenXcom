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
// Ported from: src/Engine/Yaml.h (YamlNodeWriter, YamlRootNodeWriter)

using System.Collections;
using System.Runtime.CompilerServices;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Acom;

/// <summary>Mutable node used while writing YAML.</summary>
internal sealed class YamlWriteNode
{
	public enum NodeKind
	{
		None,
		Scalar,
		Map,
		Seq,
	}

	public NodeKind Kind { get; set; }
	public string? Key { get; init; }
	public string? Value { get; set; }
	public bool Null { get; set; }
	public bool Flow { get; set; }
	public bool Quoted { get; set; }
	public List<YamlWriteNode> Children { get; } = [];
}

/// <summary>
/// Write access to a YAML node (C++ YamlNodeWriter). Writing a keyed value turns the node into a map,
/// writing an unkeyed value into a sequence.
/// </summary>
public class YamlNodeWriter
{
	private protected readonly YamlWriteNode _node;

	internal YamlNodeWriter(YamlWriteNode node)
	{
		_node = node;
	}

	/// <summary>Appends an empty child (sequence element) and returns a writer for it.</summary>
	public YamlNodeWriter Write()
	{
		var child = new YamlWriteNode();
		Append(child, keyed: false);
		return new YamlNodeWriter(child);
	}

	/// <summary>Gets the child with the given key, creating an empty one if needed.</summary>
	public YamlNodeWriter this[string key]
	{
		get
		{
			var child = _node.Children.FirstOrDefault(c => c.Key == key);
			if (child == null)
			{
				child = new YamlWriteNode { Key = key };
				Append(child, keyed: true);
			}
			return new YamlNodeWriter(child);
		}
	}

	/// <summary>Appends a value as sequence element.</summary>
	public YamlNodeWriter Write<T>(T inputValue)
	{
		var child = new YamlWriteNode();
		Set(child, inputValue);
		Append(child, keyed: false);
		return new YamlNodeWriter(child);
	}

	/// <summary>Appends a key/value pair.</summary>
	public YamlNodeWriter Write<T>(string key, T inputValue)
	{
		var child = new YamlWriteNode { Key = key };
		Set(child, inputValue);
		Append(child, keyed: true);
		return new YamlNodeWriter(child);
	}

	/// <summary>Writes a list as sequence under a key, element by element via <paramref name="callback"/>; nothing for an empty list.</summary>
	public void Write<T>(string key, IReadOnlyCollection<T> inputVector, Action<YamlNodeWriter, T> callback)
	{
		ArgumentNullException.ThrowIfNull(inputVector);
		ArgumentNullException.ThrowIfNull(callback);
		if (inputVector.Count == 0)
			return;
		var sequence = new YamlWriteNode { Key = key, Kind = YamlWriteNode.NodeKind.Seq };
		Append(sequence, keyed: true);
		var writer = new YamlNodeWriter(sequence);
		foreach (var element in inputVector)
			callback(writer, element);
	}

	/// <summary>Writes the value only if it differs from the default.</summary>
	public void TryWrite<T>(string key, T inputValue, T defaultValue)
	{
		if (!EqualityComparer<T>.Default.Equals(inputValue, defaultValue))
			Write(key, inputValue);
	}

	/// <summary>Sets this node's value.</summary>
	public void SetValue<T>(T inputValue) => Set(_node, inputValue);

	public void SetValueNull()
	{
		_node.Kind = YamlWriteNode.NodeKind.Scalar;
		_node.Null = true;
		_node.Value = "~";
	}

	public void SetAsMap() => _node.Kind = YamlWriteNode.NodeKind.Map;

	public void SetAsSeq() => _node.Kind = YamlWriteNode.NodeKind.Seq;

	public void UnsetAsMap()
	{
		if (_node.Kind == YamlWriteNode.NodeKind.Map)
			_node.Kind = YamlWriteNode.NodeKind.None;
	}

	public void UnsetAsSeq()
	{
		if (_node.Kind == YamlWriteNode.NodeKind.Seq)
			_node.Kind = YamlWriteNode.NodeKind.None;
	}

	/// <summary>Emit this container inline: <c>[1, 2]</c> / <c>{a: 1}</c>.</summary>
	public void SetFlowStyle() => _node.Flow = true;

	public void SetBlockStyle() => _node.Flow = false;

	/// <summary>Emit this scalar double-quoted with escapes (used for strings with control characters).</summary>
	public void SetAsQuotedAndEscaped() => _node.Quoted = true;

	/// <summary>Converts the written tree into a reader.</summary>
	public YamlNodeReader ToReader() => new YamlRootNodeReader(Emit(), "writer").ToBase();

	/// <summary>Adds a key with binary data encoded as base64 (C++ writeBase64).</summary>
	public YamlNodeWriter WriteBase64(string key, ReadOnlySpan<byte> data) => Write(key, Convert.ToBase64String(data));

	/// <summary>Serializes this node to YAML text.</summary>
	public string Emit() => YamlEmitter.Emit(_node);

	private void Append(YamlWriteNode child, bool keyed)
	{
		if (_node.Kind is YamlWriteNode.NodeKind.None or YamlWriteNode.NodeKind.Scalar)
		{
			_node.Kind = keyed ? YamlWriteNode.NodeKind.Map : YamlWriteNode.NodeKind.Seq;
			_node.Value = null;
		}
		_node.Children.Add(child);
	}

	private static void Set<T>(YamlWriteNode node, T value)
	{
		switch (value)
		{
			case null:
				node.Kind = YamlWriteNode.NodeKind.Scalar;
				node.Null = true;
				node.Value = "~";
				return;
			case string s:
				node.Kind = YamlWriteNode.NodeKind.Scalar;
				node.Value = s;
				return;
			case IDictionary dict:
				node.Kind = YamlWriteNode.NodeKind.Map;
				foreach (DictionaryEntry e in dict)
				{
					var child = new YamlWriteNode { Key = YamlConvert.FormatScalar(e.Key) };
					Set(child, e.Value);
					node.Children.Add(child);
				}
				return;
			case ITuple tuple when value.GetType().FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal):
				node.Kind = YamlWriteNode.NodeKind.Seq;
				for (int i = 0; i < tuple.Length; ++i)
				{
					var child = new YamlWriteNode();
					Set(child, tuple[i]);
					node.Children.Add(child);
				}
				return;
			case IEnumerable list:
				node.Kind = YamlWriteNode.NodeKind.Seq;
				foreach (var element in list)
				{
					var child = new YamlWriteNode();
					Set(child, element);
					node.Children.Add(child);
				}
				return;
			default:
				if (YamlConvert.CustomWriter(value.GetType()) is { } custom)
				{
					custom.Invoke(null, [new YamlNodeWriter(node), value]);
					return;
				}
				node.Kind = YamlWriteNode.NodeKind.Scalar;
				node.Value = YamlConvert.FormatScalar(value);
				return;
		}
	}
}

/// <summary>Root of a YAML document being written (C++ YamlRootNodeWriter).</summary>
public sealed class YamlRootNodeWriter : YamlNodeWriter
{
	public YamlRootNodeWriter() : base(new YamlWriteNode())
	{
	}

	/// <summary>The root as plain writer.</summary>
	public YamlNodeWriter ToBase() => this;
}

/// <summary>Turns node trees into YAML text (block style, indented sequences, like rapidyaml's emitter).</summary>
internal static class YamlEmitter
{
	public static string Emit(YamlWriteNode root)
	{
		var text = new StringWriter();
		var emitter = new Emitter(text, EmitterSettings.Default.WithBestIndent(2).WithBestWidth(int.MaxValue).WithIndentedSequences());
		emitter.Emit(new StreamStart());
		emitter.Emit(new DocumentStart());
		EmitNode(emitter, root);
		emitter.Emit(new DocumentEnd(isImplicit: true));
		emitter.Emit(new StreamEnd());
		return text.ToString();
	}

	private static void EmitNode(IEmitter emitter, YamlWriteNode node)
	{
		switch (node.Kind)
		{
			case YamlWriteNode.NodeKind.Map:
				emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, node.Flow ? MappingStyle.Flow : MappingStyle.Block));
				foreach (var child in node.Children)
				{
					emitter.Emit(new Scalar(child.Key ?? ""));
					EmitNode(emitter, child);
				}
				emitter.Emit(new MappingEnd());
				break;
			case YamlWriteNode.NodeKind.Seq:
				emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, isImplicit: true, node.Flow ? SequenceStyle.Flow : SequenceStyle.Block));
				foreach (var child in node.Children)
					EmitNode(emitter, child);
				emitter.Emit(new SequenceEnd());
				break;
			case YamlWriteNode.NodeKind.Scalar when node.Null:
				emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, "~", ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false));
				break;
			default:
				var style = node.Quoted ? ScalarStyle.DoubleQuoted : ScalarStyle.Any;
				emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, node.Value ?? "", style, isPlainImplicit: true, isQuotedImplicit: true));
				break;
		}
	}

	/// <summary>Converts a parsed node back into a writable node (used by YamlNodeReader.Emit).</summary>
	public static YamlWriteNode FromReader(YamlTreeNode node)
	{
		var result = new YamlWriteNode
		{
			Key = node.Key,
			Kind = node.Kind switch
			{
				YamlKind.Map => YamlWriteNode.NodeKind.Map,
				YamlKind.Seq => YamlWriteNode.NodeKind.Seq,
				_ => YamlWriteNode.NodeKind.Scalar,
			},
			Value = node.Kind == YamlKind.Scalar ? node.Value : null,
			Null = node.IsNull && node.Value != "",
			Quoted = node.Style is ScalarStyle.DoubleQuoted or ScalarStyle.SingleQuoted,
		};
		foreach (var child in node.Children)
			result.Children.Add(FromReader(child));
		return result;
	}
}
