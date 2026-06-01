# -*- coding: utf-8 -*-
import re

filepath = r'd:\G\GHR\Assets\Editor\SceneBuilder\Chapter1SceneBuilder.cs'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the line with invNames and fix the escaping
# The current file has \\\\n which in C# source is \\n (literal backslash + n)
# We want just \n (actual newline in C#)
# In the file bytes, \\\\n appears as 4 backslash chars + n
# We need to replace with just \n (one backslash + n in the file)

lines = content.split('\n')
for i, line in enumerate(lines):
    if 'invNames' in line and 'string[]' in line:
        # Replace all occurrences of \\\\n with \n in this line
        # The file currently has the literal characters: \ \ \ \ n
        # We want just: \ n
        fixed = line.replace('\\\\\\\\n', '\\n')
        if fixed == line:
            # Try with fewer backslashes
            fixed = line.replace('\\\\n', '\\n')
        lines[i] = fixed
        print(f"Before: {repr(line)}")
        print(f"After:  {repr(fixed)}")
        break

with open(filepath, 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines))
print("Done!")
