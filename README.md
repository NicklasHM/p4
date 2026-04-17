# RAL — Resource Availability Language

A domain-specific language (DSL) for expressing and querying resource availability.
Developed as a semester project at Aalborg University, SW4 Group 2, 2026.


## Project Structure

```
p4/
├── CocoR/               # Coco/R grammar and generation script
├── src/
│   └── RAL/             # C# project
│       ├──  Generated/  # Auto-generated scanner and parser (do not edit)
│       ├──  AST
│       └──  Interpreter
├── tests/
│   ├── parser/
│   ├── typechecker/
│   ├── semantics/
│   └── integration/

```

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Coco/R](https://ssw.jku.at/Research/Projects/Coco/) — only needed if regenerating the parser
- Git

### Setup

Clone the repository and navigate to the project:

```bash
git clone https://github.com/Jacob580234/p4.git
cd p4
```

### Regenerating the Parser

Run the generation script whenever the grammar has been updated:

```bash
cd CocoR
./generate.ps1
```

This generates `Scanner.cs` and `Parser.cs` directly into `src/RAL/Generated/`.

### Running a RAL Program

```bash
cd src/RAL
dotnet run -- ../../tests/parser/valid/yourfile.ra
```

## Example
```bash
category Room;
category DoubleRoom is a Room;
DoubleRoom room205 {
  bool seaView;
  int floor;
};

check room205 from 15/03-2026 14:00 to 17/03-2026 12:00;
```

## Status

Work in progress — parser and grammar are functional.
Interpreter and semantics are under development.

## Group Members

- Abtin Sedigh Rezvani
- Jacob Alexander Byrdal
- Jacob Kjærsgaard Sand
- Mathias Emborg
- Mihnea Christian Spinu
- Nicklas Holm Mikkelsen
