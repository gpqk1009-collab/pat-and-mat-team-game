"""Offline packaging/structure checks only. This is NOT a C# compiler or Unity test."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets' / 'PrettyGenocide'


def check_delimiters(path: Path) -> None:
    text = path.read_text(encoding='utf-8')
    # All supplied sources use ordinary C# strings; there are no raw/verbatim literals.
    scrubbed = re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'',
                      lambda match: '\n' * match.group().count('\n'), text, flags=re.S)
    pairs = {')': '(', ']': '[', '}': '{'}
    stack = []
    for index, ch in enumerate(scrubbed):
        if ch in '([{':
            stack.append((ch, index))
        elif ch in ')]}':
            assert stack and stack.pop()[0] == pairs[ch], f'{path.name}: unmatched {ch}'
    assert not stack, f'{path.name}: unclosed delimiter'
    directives = []
    for line in text.splitlines():
        line = line.strip()
        if line.startswith('#if '):
            directives.append(line)
        elif line in ('#else',) or line.startswith('#elif '):
            assert directives, f'{path.name}: unmatched {line}'
        elif line == '#endif':
            assert directives, f'{path.name}: unmatched #endif'
            directives.pop()
    assert not directives, f'{path.name}: unclosed #if'


def main() -> None:
    files = sorted(SOURCE.rglob('*.cs'))
    assert len(files) == 12, f'Expected 12 C# files, found {len(files)}'
    definitions = set()
    for path in files:
        check_delimiters(path)
        text = path.read_text(encoding='utf-8')
        names = re.findall(r'\bclass\s+(PG\w+)', text)
        assert path.stem in names, f'{path.name}: class/file mismatch'
        definitions.update(names)
        if path.parent.name == 'Runtime':
            assert 'using UnityEditor' not in text, f'{path.name}: editor dependency in runtime'
    all_text = '\n'.join(path.read_text(encoding='utf-8') for path in files)
    references = set(re.findall(r'\bPG[A-Z][A-Za-z0-9]*\b', all_text))
    assert references <= definitions, f'Unresolved PG names: {references - definitions}'
    assert 'Input.GetAxis' not in all_text and 'Input.GetMouseButton' not in all_text
    builder = (SOURCE / 'Editor/PGPrototypeSceneBuilder.cs').read_text(encoding='utf-8')
    assert 'GenerateUniqueAssetPath' in builder
    for layer in ('PG_Player', 'PG_Target', 'PG_Bullet', 'PG_EnemyBullet'):
        assert f'EnsureLayer("{layer}")' in builder
    for document in ('README_KO.md', 'AGENTS.md', 'CODEX_PROMPTS_KO.md', 'QA_KO.md'):
        assert (ROOT / document).is_file(), f'Missing {document}'
    print(f'PASS: {len(files)} C# files; delimiters, preprocessor balance, class/file names,')
    print('      PG type references, runtime/editor separation, required documentation.')
    print('NOT RUN: C# compilation, Unity scene creation, ledger C# tests, Play Mode or build.')


if __name__ == '__main__':
    main()
