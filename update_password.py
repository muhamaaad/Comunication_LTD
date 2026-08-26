import sys
import re

password = sys.argv[1]
files = [
    r'vulnerable\comunication_ltd\settings.py',
    r'secure\comunication_ltd\settings.py',
]

for f in files:
    with open(f, 'r', encoding='utf-8') as fh:
        content = fh.read()
    
    # Only replace the DB PASSWORD line, not EMAIL_HOST_PASSWORD or others
    new_content = re.sub(
        r"('PASSWORD':\s*')[^']*(')",
        r"\g<1>" + password.replace("\\", "\\\\") + r"\2",
        content,
        count=1
    )
    
    with open(f, 'w', encoding='utf-8') as fh:
        fh.write(new_content)

print("Password updated in both settings.py")
