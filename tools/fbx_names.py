import re, sys
p = sys.argv[1]
data = open(p, "rb").read()
# printable runs
text = "".join(chr(b) if 32 <= b < 127 else "\n" for b in data)
for m in re.finditer(r"Model::[A-Za-z0-9_]+|Geometry::[A-Za-z0-9_]+", text):
    print(m.group(0))
