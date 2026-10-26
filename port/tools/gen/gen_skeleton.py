#!/usr/bin/env python3
"""Generates C# skeletons (API stubs) for the C++ units that are not ported yet.

Reads all headers under src/ with libclang (function bodies skipped) and writes one file per open unit,
port/src/Acom.Core/<Area>/<Name>.cs, with the public/protected API of its header: classes, structs,
enums, constants, fields, constructors, methods and properties. Bodies throw NotImplementedException.
The skeletons keep the port compilable and fix the C# names, so units can be ported independently;
porting a unit replaces its skeleton file (marked "// Port status: stub"). Existing files that are not
stubs are never touched.

Naming (docs/architecture/csharp-port.md): getX()/isX()/hasX() without parameters -> property X/IsX/HasX
(with a setter if setX(T) exists), other methods PascalCase, fields and constants keep their C++ names.

Usage: gen_skeleton.py [--report]      (--report: list unmapped types and free functions only)
Requires: pip install libclang==18.1.1
"""
import re
import sys
from pathlib import Path

import clang.cindex as ci

ROOT = Path(__file__).resolve().parents[3]
CS = ROOT / "port" / "src" / "Acom.Core"
CK = ci.CursorKind
STUB_MARK = "// Port status: stub"
# Units without skeletons: the OXCE script engine (ported by hand) and the headers of already ported code.
NO_SKELETON = re.compile(r"^src/(Engine/Script|Mod/ModScript|Engine/Options\.inc)")
SCRIPT_TYPES = re.compile(r"\b(Script\w*|ModScript\w*|\w*ScriptParser\w*|ScriptValues|ScriptGlobal|ScriptTag|ScriptWorker\w*|ScriptContainer\w*|ScriptOutputArgs|ScriptFunc|ProgPos|RetEnum)\b")

# C++ types that become C# structs (small, copied by value, no identity).
VALUE_TYPES = {
	"Position", "UnitStats", "ArmorMoveCost",
	"PathfindingCost", "PathfindingStep", "LastPositions", "UnitWalkingOffset", "OpenSetEntry",
	"RuleItemFuseTrigger", "BattlescapeTally", "SDL_Rect", "Rgba", "EnvironmentalCondition", "RuleItemUseFlat",
}

PRIM = {
	"void": "void", "bool": "bool", "char": "char", "signed char": "sbyte", "unsigned char": "byte",
	"short": "short", "short int": "short", "unsigned short": "ushort", "unsigned short int": "ushort",
	"int": "int", "signed": "int", "signed int": "int", "unsigned": "uint", "unsigned int": "uint",
	"long": "long", "long int": "long", "unsigned long": "ulong", "unsigned long int": "ulong",
	"long long": "long", "long long int": "long", "unsigned long long": "ulong", "unsigned long long int": "ulong",
	"float": "float", "double": "double", "long double": "double", "char32_t": "char", "char16_t": "char", "wchar_t": "char",
}
NAMED = {
	"Uint8": "byte", "Sint8": "sbyte", "Uint16": "ushort", "Sint16": "short", "Uint32": "uint", "Sint32": "int",
	"Uint64": "ulong", "Sint64": "long", "size_t": "int", "std::size_t": "int", "ssize_t": "int", "time_t": "long",
	"int8_t": "sbyte", "uint8_t": "byte", "int16_t": "short", "uint16_t": "ushort", "int32_t": "int",
	"uint32_t": "uint", "int64_t": "long", "uint64_t": "ulong", "std::int64_t": "long", "std::uint64_t": "ulong",
	"intptr_t": "nint", "uintptr_t": "nuint", "UCode": "char", "UString": "string",
	"std::string": "string", "std::wstring": "string", "std::string_view": "string", "std::nullptr_t": "object",
	"SDL_Color": "Rgba", "SDL_Rect": "SDL_Rect", "SDL_Event": "SDL_Event", "SDLKey": "SDLKey", "SDLMod": "SDLMod",
	"YAML::YamlNodeReader": "YamlNodeReader", "YAML::YamlNodeWriter": "YamlNodeWriter",
	"YAML::YamlRootNodeReader": "YamlRootNodeReader", "YAML::YamlRootNodeWriter": "YamlRootNodeWriter",
	"YAML::YamlString": "string", "ActionHandler": "ActionHandler", "StateHandler": "StateHandler",
	"SurfaceHandler": "SurfaceHandler", "SDL_Surface": "Surface", "Vector3i": "Position",
}
SEQ = {"std::vector": "List", "std::list": "List", "std::deque": "List", "std::forward_list": "List"}
CS_KEYWORDS = set("""abstract as base bool break byte case catch char checked class const continue decimal default
delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int
interface internal is lock long namespace new null object operator out override params private protected public
readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof
uint ulong unchecked unsafe ushort using virtual void volatile while""".split())


def pascal(name):
	return name[:1].upper() + name[1:]


def ident(name):
	return "@" + name if name in CS_KEYWORDS else name


# ---------------------------------------------------------------- C++ type strings -> C#

TOKEN = re.compile(r"::|<|>|,|\*|&&|&|\(|\)|\[|\]|[A-Za-z_][A-Za-z_0-9]*|\d+|\S")
BUILTIN_WORDS = {"unsigned", "signed", "long", "short", "int", "char", "double", "float", "bool", "void", "char32_t", "char16_t", "wchar_t"}


class TypeNode:
	def __init__(self, name, args, ptr, ref, const, array):
		self.name, self.args, self.ptr, self.ref, self.const, self.array = name, args, ptr, ref, const, array


def parse_type(spelling):
	toks = TOKEN.findall(spelling.replace("struct ", "").replace("class ", "").replace("enum ", ""))
	pos = [0]

	def peek():
		return toks[pos[0]] if pos[0] < len(toks) else None

	def take():
		pos[0] += 1
		return toks[pos[0] - 1]

	def node():
		const = False
		while peek() in ("const", "volatile", "typename"):
			const |= take() == "const"
		if peek() in BUILTIN_WORDS:
			words = []
			while peek() in BUILTIN_WORDS:
				words.append(take())
			name, args = " ".join(words), []
		else:
			parts, args = [], []
			while True:
				if peek() is None:
					break
				parts.append(take())
				if peek() == "<":
					take()
					args = []
					while peek() not in (">", None):
						if peek() == ",":
							take()
							continue
						if peek() and peek()[0].isdigit():
							args.append(TypeNode(take(), [], 0, False, False, False))
							continue
						args.append(node())
					take()
				if peek() == "::":
					parts.append(take())
					continue
				break
			name = "".join(parts)
		ptr, ref, array = 0, False, False
		while peek() in ("*", "&", "&&", "const", "[", "volatile"):
			t = take()
			if t == "*":
				ptr += 1
			elif t in ("&", "&&"):
				ref = True
			elif t == "const":
				const = True
			elif t == "[":
				while peek() not in ("]", None):
					take()
				take()
				array = True
		if peek() == "(":  # function pointer
			raise ValueError("function type")
		return TypeNode(name, args, ptr, ref, const, array)

	n = node()
	if pos[0] != len(toks):
		raise ValueError("trailing tokens in " + spelling)
	return n


class Mapper:
	def __init__(self, model):
		self.m = model
		self.unknown = {}

	def cs_name(self, name, where):
		"""Maps a (possibly qualified) C++ type name to a known C# type name, or None."""
		n = name.removeprefix("::").removeprefix("OpenXcom::")
		if n in NAMED:
			return NAMED[n]
		if n in self.m.typedefs:
			return self.typedef(n)
		# nested name relative to the enclosing classes of the use site
		scopes = where.split(".") if where else []
		for i in range(len(scopes), -1, -1):
			cand = ".".join(scopes[:i] + n.split("::"))
			if cand in self.m.types:
				return cand
			if cand in self.m.typedefs:
				return self.typedef(cand)
		return None

	def typedef(self, name):
		v = self.m.typedefs[name]
		if isinstance(v, tuple):
			self.m.typedefs[name] = "object"  # guards recursion
			v = self.m.typedefs[name] = self.map(v[1], v[2])[0]
		return v

	def map(self, spelling, where="", role="field"):
		"""Returns (C# type, by_ref) for a C++ type spelling; role is field, ret or param."""
		try:
			n = parse_type(spelling)
		except ValueError:
			return self.fallback(spelling, where), False
		return self.node(n, where, role)

	def fallback(self, spelling, where):
		self.unknown.setdefault(spelling, where)
		return "object"

	def node(self, n, where, role):
		if n.name in PRIM:
			base = PRIM[n.name]
			if n.name == "char" and n.ptr == 1:
				return "string", False
			if n.ptr == 0 and not n.array:
				return base, role == "param" and n.ref and not n.const
			if n.array or n.ptr == 1 and base == "byte":
				return base + "[]", False
			if base == "void":
				return "object", False
			return base, role == "param"
		if n.name.startswith("std::") and n.name not in NAMED:
			return self.std(n, where), False
		cs = self.cs_name(n.name, where)
		if cs is None:
			return self.fallback(n.name, where), False
		if n.array:
			return cs + "[]", False
		if cs == "Rgba" and n.ptr == 1:
			return ("ReadOnlySpan<Rgba>" if role == "param" else "Rgba[]"), False
		value = cs.split(".")[-1] in VALUE_TYPES or cs in self.m.enums or cs in ("string", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "char", "bool", "nint", "nuint")
		by_ref = role == "param" and value and ((n.ref and not n.const) or (n.ptr == 1 and not n.const and cs != "string"))
		return cs, by_ref

	def arg(self, n, where):
		return self.node(n, where, "arg")[0]

	def std(self, n, where):
		name = n.name
		a = n.args
		if name in SEQ and a:
			return "List<" + self.arg(a[0], where) + ">"
		if name in ("std::map", "std::multimap", "std::unordered_map") and len(a) >= 2:
			k, v = self.arg(a[0], where), self.arg(a[1], where)
			ordered = name != "std::unordered_map" and (a[0].ptr == 0 and (k in ("string", "int", "uint", "char", "byte", "short", "ushort", "long", "ulong") or k in self.m.enums))
			return ("SortedDictionary<" if ordered else "Dictionary<") + k + ", " + v + ">"
		if name in ("std::set", "std::multiset", "std::unordered_set") and a:
			k = self.arg(a[0], where)
			ordered = name != "std::unordered_set" and a[0].ptr == 0 and (k in ("string", "int", "uint") or k in self.m.enums)
			return ("SortedSet<" if ordered else "HashSet<") + k + ">"
		if name == "std::pair" and len(a) == 2:
			return "(" + self.arg(a[0], where) + " first, " + self.arg(a[1], where) + " second)"
		if name == "std::tuple" and len(a) >= 2:
			return "(" + ", ".join(self.arg(x, where) for x in a) + ")"
		if name == "std::array" and a:
			return self.arg(a[0], where) + "[]"
		if name in ("std::unique_ptr", "std::shared_ptr", "std::weak_ptr") and a:
			return self.arg(a[0], where)
		if name == "std::bitset":
			return "System.Collections.BitArray"
		if name == "std::basic_string":
			return "string"
		return self.fallback(name, where)


# ---------------------------------------------------------------- model


class Member:
	def __init__(self, **kw):
		self.__dict__.update(kw)


class TypeDecl:
	def __init__(self, cursor, name, kind, header, outer):
		self.cursor, self.name, self.kind, self.header, self.outer = cursor, name, kind, header, outer
		self.full = (outer.full + "." if outer else "") + name
		self.bases, self.nested, self.fields, self.methods, self.ctors, self.consts = [], [], [], [], [], []
		self.enum_items, self.enum_base = [], "int"
		self.abstract = False


class Model:
	def __init__(self):
		self.types = {}  # C# full name -> TypeDecl or "existing"
		self.enums = set()
		self.enum_consts = {}  # C++ constant name (qualified by scope) -> C# expression
		self.typedefs = {}
		self.by_header = {}
		self.free = {}  # header -> [cursor]
		self.existing_members = {}  # C# class -> {name: "prop"|"method"}


def header_of(cursor):
	f = cursor.location.file
	if not f:
		return None
	p = Path(f.name).resolve()
	try:
		rel = p.relative_to(ROOT).as_posix()
	except ValueError:
		return None
	return rel if rel.startswith("src/") else None


def ported_units():
	status = {}
	for f in CS.rglob("*.cs"):
		if "/obj/" in f.as_posix() or "/bin/" in f.as_posix():
			continue
		text = f.read_text(encoding="utf-8", errors="replace")
		m = re.search(r"^// Ported from: (.+)$", text, re.M)
		if not m:
			continue
		stub = STUB_MARK in text
		for path in m.group(1).split(","):
			path = path.strip().split(" ")[0]
			if path.startswith("src/"):
				status[path.rsplit(".", 1)[0]] = "stub" if stub else "ported"
	return status


def skipped_globs():
	import fnmatch
	pats = []
	for line in (ROOT / "port" / "tools" / "port-skip.txt").read_text(encoding="utf-8").splitlines():
		if line.strip() and not line.startswith("#"):
			pats.append(line.partition("|")[0].strip())
	return lambda unit: any(fnmatch.fnmatch(unit, p) for p in pats)


def scan_existing_cs(model):
	"""Type and member names of hand-ported C# code (they win over the C++ naming rules)."""
	for f in CS.rglob("*.cs"):
		if "/obj/" in f.as_posix() or "/bin/" in f.as_posix():
			continue
		text = f.read_text(encoding="utf-8", errors="replace")
		if STUB_MARK in text:
			continue
		current = None
		for line in text.splitlines():
			m = re.match(r"^(public|internal)\s+(?:(?:sealed|static|abstract|readonly|partial|record)\s+)*(class|struct|enum|interface|delegate\s+\S+)\s+(\w+)", line)
			if m:
				current = m.group(3)
				model.types.setdefault(current, "existing")
				if m.group(2) == "enum":
					model.enums.add(current)
				model.existing_members.setdefault(current, {})
				continue
			m = re.match(r"^\t(public|protected|internal)\s+(?:(?:static|virtual|override|abstract|sealed|new|readonly|const)\s+)*[\w<>\[\], ?.()]+?\s+(\w+)\s*(\(|\{|=>|;|=)", line)
			if m and current:
				model.existing_members[current][m.group(2)] = ("method" if m.group(3) == "(" else "prop", "protected" if m.group(1) == "protected" else "public")


def access_ok(c):
	return c.access_specifier in (ci.AccessSpecifier.PUBLIC, ci.AccessSpecifier.PROTECTED)


def access_cs(c):
	return "protected" if c.access_specifier == ci.AccessSpecifier.PROTECTED else "public"


def collect(model, tu):
	def visit_type(c, header, outer):
		name = c.spelling
		if not name or not c.is_definition():
			return None
		kind = {CK.CLASS_DECL: "class", CK.STRUCT_DECL: "struct", CK.ENUM_DECL: "enum", CK.UNION_DECL: "union"}[c.kind]
		t = TypeDecl(c, name, kind, header, outer)
		model.types[t.full] = t
		if kind == "enum":
			model.enums.add(t.full)
			ut = c.enum_type.spelling
			t.enum_base = Mapper(model).map(ut)[0] if ut else "int"
			if t.enum_base not in ("byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong"):
				t.enum_base = "int"
			scope = outer.full + "." if outer else ""
			for e in c.get_children():
				if e.kind == CK.ENUM_CONSTANT_DECL:
					t.enum_items.append((e.spelling, e.enum_value))
					model.enum_consts[scope + e.spelling] = t.full + "." + e.spelling
			return t
		for ch in c.get_children():
			if ch.kind in (CK.CLASS_DECL, CK.STRUCT_DECL, CK.ENUM_DECL) and ch.is_definition():
				if ch.spelling:
					n = visit_type(ch, header, t)
					if n:
						t.nested.append(n)
				elif ch.kind == CK.ENUM_DECL:  # anonymous enum -> constants
					for e in ch.get_children():
						if e.kind == CK.ENUM_CONSTANT_DECL:
							t.consts.append(Member(name=e.spelling, type="int", value=str(e.enum_value), access="public", static=True, const=True))
							model.enum_consts[t.full + "." + e.spelling] = t.full + "." + e.spelling
			elif ch.kind in (CK.TYPEDEF_DECL, CK.TYPE_ALIAS_DECL):
				model.typedefs.setdefault(t.full + "." + ch.spelling, ("pending", ch.underlying_typedef_type.spelling, t.full))
		return t

	def visit_ns(c, ns):
		for ch in c.get_children():
			header = header_of(ch)
			if not header:
				continue
			if ch.kind == CK.NAMESPACE:
				visit_ns(ch, ns + [ch.spelling])
			elif ch.kind in (CK.CLASS_DECL, CK.STRUCT_DECL, CK.ENUM_DECL) and ch.is_definition() and ch.spelling and ns[:1] == ["OpenXcom"] and len(ns) == 1:
				t = visit_type(ch, header, None)
				if t:
					model.by_header.setdefault(header, []).append(t)
			elif ch.kind in (CK.TYPEDEF_DECL, CK.TYPE_ALIAS_DECL) and ns[:1] == ["OpenXcom"]:
				model.typedefs.setdefault(ch.spelling, ("pending", ch.underlying_typedef_type.spelling, ""))
			elif ch.kind == CK.FUNCTION_DECL and ns[:1] == ["OpenXcom"]:
				model.free.setdefault(header, []).append((ns[1:], ch))

	visit_ns(tu.cursor, [])


def resolve_typedefs(model, mapper):
	for _ in range(5):
		for k, v in list(model.typedefs.items()):
			if isinstance(v, tuple):
				_, spelling, where = v
				model.typedefs[k] = "object"  # guards recursion
				cs = mapper.map(spelling, where)[0]
				model.typedefs[k] = cs


# ---------------------------------------------------------------- members


def default_value(parm, cs_type, mapper, where):
	toks = [t.spelling for t in parm.get_tokens()]
	if "=" not in toks:
		return None
	expr = toks[toks.index("=") + 1:]
	s = "".join(expr)
	simple = cs_type in ("int", "uint", "long", "ulong", "short", "ushort", "byte", "sbyte", "double", "float", "char", "bool", "string") or cs_type in mapper.m.enums
	if not simple and s not in ("nullptr", "NULL", "{}") and not re.fullmatch(r"\w+\(\)", s):
		return None
	if s in ("nullptr", "NULL") or (s == "0" and cs_type not in ("int", "uint", "long", "ulong", "short", "ushort", "byte", "sbyte", "double", "float", "char") and cs_type not in mapper.m.enums):
		return "null" if cs_type not in mapper.m.enums else "default"
	if s in ("true", "false"):
		return s
	if re.fullmatch(r"-?\d+[uUlL]*", s) or re.fullmatch(r"-?0x[0-9a-fA-F]+[uUlL]*", s):
		v = s.rstrip("uUlL")
		if cs_type in mapper.m.enums:
			return "(" + cs_type + ")" + v if not v.startswith("-") else "(" + cs_type + ")(" + v + ")"
		if v.startswith("-") and cs_type in ("uint", "ulong", "ushort", "byte"):
			return None
		if cs_type == "string":
			return None
		return v
	if re.fullmatch(r"-?\d*\.\d*([eE][-+]?\d+)?[fF]?", s):
		return s.rstrip("fF") + ("f" if cs_type == "float" else "")
	if re.fullmatch(r'"[^"]*"', s) or s in ("std::string()", "\"\"", "{}") and cs_type == "string":
		return s if s.startswith('"') else '""'
	if s == "{}" or re.fullmatch(r"\w+\(\)", s):
		return "default"
	name = s.removeprefix("OpenXcom::")
	scopes = where.split(".") if where else []
	for i in range(len(scopes), -1, -1):
		cand = ".".join(scopes[:i] + name.split("::"))
		if cand in mapper.m.enum_consts:
			return mapper.m.enum_consts[cand]
	if name.startswith("SDL_BUTTON_"):
		return "SDLMouse." + name
	return None


def method_params(c, mapper, where):
	params = []
	for p in c.get_arguments():
		t, by_ref = mapper.map(p.type.spelling, where, "param")
		params.append([("ref " if by_ref else "") + t, ident(p.spelling or "arg%d" % len(params)), default_value(p, t, mapper, where), p.type.spelling])
	# C# needs all optional parameters at the end
	ok = True
	for p in reversed(params):
		if p[2] is None or p[0].startswith("ref "):
			ok = False
		if not ok:
			p[2] = None
	return params


def is_script(spelling):
	return bool(SCRIPT_TYPES.search(spelling))


def collect_members(model, t, mapper):
	c = t.cursor
	for ch in c.get_children():
		if ch.kind == CK.CXX_BASE_SPECIFIER:
			name = ch.type.spelling.removeprefix("OpenXcom::")
			cs = mapper.cs_name(name, t.outer.full if t.outer else "")
			if cs and cs not in ("object",):
				t.bases.append(cs)
		elif ch.kind == CK.FIELD_DECL and (access_ok(ch) or t.kind == "struct" and ch.access_specifier == ci.AccessSpecifier.PUBLIC):
			if is_script(ch.type.spelling):
				continue
			typ = mapper.map(ch.type.spelling, t.full)[0]
			t.fields.append(Member(name=ident(ch.spelling), type=typ, access=access_cs(ch)))
		elif ch.kind == CK.VAR_DECL and access_ok(ch):
			if is_script(ch.type.spelling):
				continue
			typ = mapper.map(ch.type.spelling, t.full)[0]
			toks = [x.spelling for x in ch.get_tokens()]
			value = None
			if "=" in toks:
				expr = toks[toks.index("=") + 1:]
				if expr and expr[-1] == ";":
					expr = expr[:-1]
				value = const_expr(expr, model, t.full)
			const = ch.type.is_const_qualified() or "constexpr" in toks
			t.consts.append(Member(name=ident(ch.spelling), type=typ, value=value, access=access_cs(ch), static=True, const=const and value is not None))
		elif ch.kind == CK.CONSTRUCTOR and access_ok(ch):
			args = list(ch.get_arguments())
			if len(args) == 1 and ch.spelling in args[0].type.spelling and ("&" in args[0].type.spelling):
				continue  # copy/move constructor
			if any(is_script(a.type.spelling) for a in args):
				continue
			if t.name in VALUE_TYPES and not args:
				continue
			t.ctors.append(Member(params=method_params(ch, mapper, t.full), access=access_cs(ch), cursor=ch))
		elif ch.kind == CK.CXX_METHOD:
			name = ch.spelling
			if name.startswith("operator") or is_script(ch.result_type.spelling) or any(is_script(a.type.spelling) for a in ch.get_arguments()):
				continue
			if name in ("ScriptRegister", "ScriptFill") or "script" in name.lower() and "Script" in ch.result_type.spelling:
				continue
			if not access_ok(ch) and not ch.is_virtual_method():
				continue
			ret = mapper.map(ch.result_type.spelling, t.full, "ret")[0]
			t.methods.append(Member(cpp=name, ret=ret, params=method_params(ch, mapper, t.full), access=access_cs(ch),
				static=ch.is_static_method(), virtual=ch.is_virtual_method(), pure=ch.is_pure_virtual_method(),
				const=ch.is_const_method(), cursor=ch, private=not access_ok(ch)))
			if ch.is_pure_virtual_method():
				t.abstract = True


def const_expr(expr, model, where):
	out = []
	for tok in expr:
		if re.fullmatch(r"\d+[uUlL]*|0x[0-9a-fA-F]+[uUlL]*", tok):
			out.append(tok.rstrip("uUlL"))
		elif re.fullmatch(r"\d*\.\d*([eE][-+]?\d+)?[fF]?", tok):
			out.append(tok)
		elif tok in "+-*/%()<<>>|&~" or tok in ("<<", ">>"):
			out.append(tok)
		elif tok in ("true", "false") or re.fullmatch(r'"[^"]*"', tok):
			out.append(tok)
		elif re.fullmatch(r"[A-Za-z_]\w*", tok):
			scopes = where.split(".")
			found = None
			for i in range(len(scopes), -1, -1):
				cand = ".".join(scopes[:i] + [tok])
				if cand in model.enum_consts:
					found = "(int)" + model.enum_consts[cand]
					break
				owner = ".".join(scopes[:i])
				tdecl = model.types.get(owner)
				if isinstance(tdecl, TypeDecl) and any(k.name == tok for k in tdecl.consts):
					found = tok
					break
			if not found:
				return None
			out.append(found)
		elif tok == "::":
			return None
		else:
			return None
	return " ".join(out) if out else None


# ---------------------------------------------------------------- C# member forms (property vs method)


def base_chain(model, t):
	seen = []
	todo = list(t.bases)
	while todo:
		b = todo.pop(0)
		if b in seen:
			continue
		seen.append(b)
		bt = model.types.get(b)
		if isinstance(bt, TypeDecl):
			todo.extend(bt.bases)
	return seen


def base_form(model, t, cpp_name, nparams):
	"""Form of an inherited member: (form, cs_name, virtual) from the nearest base that declares it."""
	for b in base_chain(model, t):
		bt = model.types.get(b)
		if isinstance(bt, TypeDecl):
			for m in bt.methods:
				if m.cpp == cpp_name and len(m.params) == nparams:
					return m.form, m.csname, m.virtual or m.form_override, m.access
		elif bt == "existing":
			members = model.existing_members.get(b, {})
			for csname in candidate_names(cpp_name, nparams):
				if csname in members:
					form, access = members[csname]
					if form == "prop" and nparams > 1:
						continue
					return form, csname, True, access
	return None


def candidate_names(cpp, nparams):
	names = [pascal(cpp)] if nparams else []
	if cpp.startswith(("get", "set")) and cpp[3:4].isupper():
		names.append(cpp[3:])
	if cpp.startswith(("is", "has")) and cpp[2 if cpp.startswith("is") else 3:][:1].isupper():
		names.append(pascal(cpp))
	names.append(pascal(cpp))
	return names


def assign_forms(model, t, done):
	if t.full in done:
		return
	done.add(t.full)
	for b in t.bases:
		bt = model.types.get(b)
		if isinstance(bt, TypeDecl):
			assign_forms(model, bt, done)
	nested = {n.name for n in t.nested}
	# drop const/non-const duplicates
	seen, methods = set(), []
	for m in t.methods:
		key = (m.cpp, tuple(p[0] for p in m.params), m.static)
		if key in seen:
			continue
		seen.add(key)
		methods.append(m)
	t.methods = methods
	for m in t.methods:
		m.form_override = False
		inherited = None if m.static else base_form(model, t, m.cpp, len(m.params))
		m.override = bool(inherited and m.virtual and inherited[2])
		m.hides = bool(inherited) and not m.override
		if inherited:
			m.form, m.csname = inherited[0], inherited[1]
			if inherited[3] and (m.override or m.hides):
				m.access, m.private = inherited[3], False
			if m.form == "prop" and m.cpp.startswith("set"):
				m.accessor = "set"
			elif m.form == "prop":
				m.accessor = "get"
			continue
		m.form, m.csname, m.accessor = "method", pascal(m.cpp), None
		if not m.params and m.ret != "void":
			if m.cpp.startswith("get") and m.cpp[3:4].isupper():
				m.form, m.csname, m.accessor = "prop", m.cpp[3:], "get"
			elif m.ret == "bool" and (m.cpp.startswith("is") and m.cpp[2:3].isupper() or m.cpp.startswith("has") and m.cpp[3:4].isupper()):
				m.form, m.csname, m.accessor = "prop", pascal(m.cpp), "get"
	getters = {m.csname: m for m in t.methods if m.form == "prop" and m.accessor == "get"}
	for m in t.methods:
		if m.form == "method" and m.cpp.startswith("set") and len(m.params) == 1 and m.ret == "void" and not m.override and not m.hides:
			g = getters.get(m.cpp[3:])
			if g and g.static == m.static and g.ret == m.params[0][0] and not m.params[0][2] and not g.override and not g.hides:
				m.form, m.csname, m.accessor = "prop", g.csname, "set"
	# collisions: property names must be unique and must not clash with methods, nested types or the class name
	method_names = {m.csname for m in t.methods if m.form == "method"}
	field_names = {f.name for f in t.fields} | {k.name for k in t.consts}
	for m in t.methods:
		if m.form == "prop" and not m.override and not m.hides and (m.csname in method_names or m.csname in nested or m.csname == t.name or m.csname in field_names):
			m.form, m.accessor = "method", None
			m.csname = pascal(m.cpp)
	for m in t.methods:
		if m.form == "method" and (m.csname == t.name or m.csname in nested):
			m.csname += "_"
	# a property needs a getter; lone setters stay methods
	props = {}
	for m in t.methods:
		if m.form == "prop":
			props.setdefault(m.csname, []).append(m)
	for name, ms in props.items():
		if not any(x.accessor == "get" for x in ms) and not any(x.override or x.hides for x in ms):
			for x in ms:
				x.form, x.csname, x.accessor = "method", pascal(x.cpp), None


# ---------------------------------------------------------------- output


def params_cs(params):
	out = []
	for typ, name, default, _ in params:
		out.append(typ + " " + name + (" = " + default if default is not None else ""))
	return ", ".join(out)


def base_ctor_call(model, t, ctor):
	"""Base constructor call: forwards the leading parameters if their types match, else passes defaults."""
	if not t.bases:
		return ""
	bt = model.types.get(t.bases[0])
	ctors = []
	if isinstance(bt, TypeDecl):
		ctors = [([p[0] for p in c.params if p[2] is None], [p[0] for p in c.params]) for c in bt.ctors]
	elif bt == "existing":
		ctors = existing_ctors(t.bases[0])
	ctors = [c for c in ctors if c[1] != [t.bases[0]]]  # copy constructors
	if not ctors or any(len(c[0]) == 0 for c in ctors):
		return ""
	mine = [p[0] for p in ctor.params]
	for req, full in sorted(ctors, key=lambda c: -len(c[1])):
		for k in range(len(full), len(req) - 1, -1):
			if mine[:k] == full[:k]:
				return " : base(" + ", ".join(p[1] for p in ctor.params[:k]) + ")"
	best = min(ctors, key=lambda c: len(c[0]))[0]
	return " : base(" + ", ".join("default(" + x.removeprefix("ref ") + ")" for x in best) + ")"


_existing_ctor_cache = {}


def existing_ctors(name):
	if name in _existing_ctor_cache:
		return _existing_ctor_cache[name]
	res = []
	for f in CS.rglob("*.cs"):
		if "/obj/" in f.as_posix():
			continue
		text = f.read_text(encoding="utf-8", errors="replace")
		for m in re.finditer(r"^\t(?:public|protected) " + name + r"\(([^)]*)\)", text, re.M):
			req, full = [], []
			for p in filter(None, (x.strip() for x in m.group(1).split(","))):
				typ = p.split("=")[0].strip().rsplit(" ", 1)[0].replace("?", "")
				full.append(typ)
				if "=" not in p and len(req) == len(full) - 1:
					req.append(typ)
			res.append((req, full))
	_existing_ctor_cache[name] = res
	return res


THROW = "throw new NotImplementedException()"


def emit_type(model, t, mapper, ind, out):
	pad = "\t" * ind
	if t.kind == "enum":
		base = "" if t.enum_base == "int" else " : " + t.enum_base
		out.append(f"{pad}public enum {t.name}{base}")
		out.append(pad + "{")
		for name, val in t.enum_items:
			out.append(f"{pad}\t{name} = {val},")
		out.append(pad + "}")
		return
	is_struct = t.name in VALUE_TYPES
	if not t.abstract and t.kind != "enum":
		pure, impl = set(), {m.csname for m in t.methods if not m.pure}
		for b in reversed(base_chain(model, t)):
			bt = model.types.get(b)
			if isinstance(bt, TypeDecl):
				for m in bt.methods:
					(pure.add if m.pure else pure.discard)(m.csname)
		t.abstract = bool(pure - impl)
	static_only = not t.fields and not t.ctors and all(m.static for m in t.methods) and all(k.static for k in t.consts) and not t.bases and t.methods
	kw = "struct" if is_struct else ("static class" if static_only else ("abstract class" if t.abstract else "class"))
	bases = [] if is_struct else t.bases[:1]
	out.append(f"{pad}public {kw} {t.name}" + (" : " + ", ".join(bases) if bases else ""))
	out.append(pad + "{")
	body = []
	for k in t.consts:
		if k.const:
			body.append(f"{pad}\t{k.access} const {k.type} {k.name} = {k.value};")
		else:
			body.append(f"{pad}\t{k.access} static {k.type} {k.name}" + (f" = {k.value};" if k.value else ";"))
	for f in t.fields:
		body.append(f"{pad}\t{f.access} {f.type} {f.name};")
	for c in t.ctors:
		access = "public" if is_struct else c.access
		call = "" if is_struct else base_ctor_call(model, t, c)
		body.append(f"{pad}\t{access} {t.name}({params_cs(c.params)}){call} => {THROW};")
	props = {}
	for m in t.methods:
		if m.form == "prop":
			props.setdefault(m.csname, []).append(m)
	emitted = set()
	for m in t.methods:
		if m.form == "prop":
			if m.csname in emitted:
				continue
			emitted.add(m.csname)
			ms = props[m.csname]
			getter = next((x for x in ms if x.accessor == "get"), None)
			setter = next((x for x in ms if x.accessor == "set"), None)
			typ = getter.ret if getter else setter.params[0][0]
			mods = mods_for(ms, is_struct)
			acc = []
			if getter:
				acc.append("get => " + THROW + ";")
			if setter:
				acc.append("set => " + THROW + ";")
			access = "public" if any(x.access == "public" or x.private for x in ms) or static_only else "protected"
			if mods == "abstract ":
				acc = [a.split(" =>")[0] + ";" for a in acc]
			body.append(f"{pad}\t{access} {mods}{typ} {m.csname} {{ {' '.join(acc)} }}")
		else:
			mods = mods_for([m], is_struct)
			access = "public" if m.access == "public" or m.private or static_only else "protected"
			if m.pure and not m.override:
				body.append(f"{pad}\t{access} {mods}{m.ret} {m.csname}({params_cs(m.params)});")
			else:
				body.append(f"{pad}\t{access} {mods}{m.ret} {m.csname}({params_cs(m.params)}) => {THROW};")
	for n in t.nested:
		emit_type(model, n, mapper, ind + 1, body)
	out.extend(body)
	out.append(pad + "}")


def mods_for(ms, is_struct):
	m = ms[0]
	if m.static:
		return "static "
	if is_struct:
		return ""
	if any(x.override for x in ms):
		return "override "
	if any(x.pure for x in ms):
		return "abstract "
	if any(x.virtual for x in ms):
		return "virtual "
	if any(x.hides for x in ms):
		return "new "
	return ""


def emit_free(model, header, funcs, mapper, out):
	name = Path(header).stem + "Functions"
	out.append(f"public static class {name}")
	out.append("{")
	seen = set()
	for ns, f in funcs:
		if f.spelling.startswith("operator") or f.spelling in ("read", "write") or is_script(f.result_type.spelling):
			continue
		ret = mapper.map(f.result_type.spelling, "", "ret")[0]
		params = method_params(f, mapper, "")
		key = (f.spelling, tuple(p[0] for p in params))
		if key in seen:
			continue
		seen.add(key)
		out.append(f"\tpublic static {ret} {pascal(f.spelling)}({params_cs(params)}) => {THROW};")
	out.append("}")


def license_header():
	return (ROOT / "port" / "tools" / "license-header.txt").read_text(encoding="utf-8").rstrip("\n")


def main():
	report = "--report" in sys.argv
	model = Model()
	scan_existing_cs(model)
	headers = sorted(p for p in (ROOT / "src").glob("*/*.h") if "Scalers" not in p.parts and "apple" not in p.parts and p.name != "Options.inc.h")
	src = "".join(f'#include "{h.as_posix()}"\n' for h in headers)
	index = ci.Index.create()
	args = ["-x", "c++", "-std=c++17", "-I" + str(ROOT / "src"), "-I" + str(ROOT / "libs" / "rapidyaml"),
		"-I/usr/include/SDL", "-D_REENTRANT", "-isystem", "/usr/lib/llvm-18/lib/clang/18/include"]
	tu = index.parse("all.cpp", args=args, unsaved_files=[("all.cpp", src)],
		options=ci.TranslationUnit.PARSE_SKIP_FUNCTION_BODIES | ci.TranslationUnit.PARSE_INCOMPLETE)
	errors = [d for d in tu.diagnostics if d.severity >= ci.Diagnostic.Error]
	if errors:
		sys.exit("libclang errors:\n" + "\n".join(str(d) for d in errors[:10]))
	existing = [k for k, v in model.types.items() if v == "existing"]
	collect(model, tu)
	for k in existing:
		model.types[k] = "existing"
	mapper = Mapper(model)
	resolve_typedefs(model, mapper)
	status = ported_units()
	is_skipped = skipped_globs()
	todo = []
	for header, types in sorted(model.by_header.items()):
		unit = header.rsplit(".", 1)[0]
		if status.get(unit) == "ported" or is_skipped(unit) or NO_SKELETON.match(unit):
			continue
		todo.append((header, unit, types))
	for header, unit, types in todo:
		def walk(t):
			if t.kind != "enum":
				collect_members(model, t, mapper)
				for n in t.nested:
					walk(n)
		for t in types:
			walk(t)
	done = set()
	for header, unit, types in todo:
		def forms(t):
			if t.kind != "enum":
				assign_forms(model, t, done)
				for n in t.nested:
					forms(n)
		for t in types:
			forms(t)
	todo_units = {u for _, u, _ in todo}
	free_units = {h: [x for x in f if x[1].spelling not in ("read", "write") and not x[1].spelling.startswith("operator")] for h, f in model.free.items() if h.rsplit(".", 1)[0] in todo_units}
	free_units = {h: f for h, f in free_units.items() if f}
	if report:
		print("unmapped types:")
		for k, v in sorted(mapper.unknown.items()):
			print(f"  {k}  (in {v})")
		print("free functions:", {h: [f.spelling for _, f in fs] for h, fs in free_units.items()})
		return
	lic = license_header()
	written = 0
	for header, unit, types in todo:
		area, name = unit.split("/")[1], unit.split("/")[2]
		path = CS / area / (name + ".cs")
		if path.exists() and STUB_MARK not in path.read_text(encoding="utf-8"):
			print("skip (exists, not a stub):", path.relative_to(ROOT))
			continue
		cpp = ROOT / (unit + ".cpp")
		ported_from = header + (", " + unit + ".cpp" if cpp.exists() else "")
		out = [lic, f"// Ported from: {ported_from}", STUB_MARK + " (generated by port/tools/gen/gen_skeleton.py; replace when porting)",
			"#nullable disable", "#pragma warning disable // generated skeleton", "", "namespace Acom;", ""]
		for t in types:
			emit_type(model, t, mapper, 0, out)
			out.append("")
		if header in free_units:
			emit_free(model, header, free_units[header], mapper, out)
			out.append("")
		path.parent.mkdir(parents=True, exist_ok=True)
		path.write_text("\n".join(out).rstrip("\n") + "\n", encoding="utf-8")
		written += 1
	print(f"{written} skeleton files written, {len(mapper.unknown)} unmapped C++ types (see --report)")


if __name__ == "__main__":
	main()
