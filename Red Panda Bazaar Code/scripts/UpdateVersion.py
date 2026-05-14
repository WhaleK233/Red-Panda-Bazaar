import json
import os

try:
    manifest_path = "manifest.json"
    if not os.path.exists(manifest_path):
        print(f"Error! {manifest_path} not found!")
        exit(1)

    with open(manifest_path, "r") as f:
        manifest = json.load(f)

    version = manifest["Version"]

    # 仅 experimental 分支包含 Build.N，自动递增构建号
    # main 分支格式 x.y.z，develop 分支格式 x.y.z-beta，均不递增
    if ".Build." in version:
        prefix, _, build_num = version.rpartition(".")
        num = str(int(build_num) + 1)
        new_version = f"{prefix}.{num}"
        manifest["Version"] = new_version
        with open(manifest_path, "w") as f:
            json.dump(manifest, f, indent=2)
        print(f"Update Version: {new_version}")
    else:
        print(f"Version: {version} (no build increment)")

except Exception as e:
    print(f"{e}")
