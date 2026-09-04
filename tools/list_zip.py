import os
import zipfile
import sys

p = sys.argv[1]
print("exists", os.path.exists(p), os.path.getsize(p) if os.path.exists(p) else 0)
try:
    with zipfile.ZipFile(p) as z:
        for n in z.namelist():
            print(n)
except Exception as e:
    print("err", type(e).__name__, e)
    with open(p, "rb") as f:
        print(f.read(32))
