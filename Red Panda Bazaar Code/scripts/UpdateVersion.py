import json
import os

try:
    manifest_path = "manifest.json"
    if not os.path.exists(manifest_path):
        print(f"Error! {manifest_path} not found!")
        exit(1)

    with open(manifest_path, "r") as f:
        manifest = json.load(f)

    version = manifest["Version"].split("-")
    ver, build = map(str, version)
    b, num = build.split(".")
    num = str(int(num) + 1)
    new_version = f"{ver}-{b}.{num}"

    manifest["Version"] = new_version
    with open(manifest_path, "w") as f:
        json.dump(manifest, f, indent=2)

    print(f"Update Version: {new_version}")

except Exception as e:
    print(f"{e}")
