#!/usr/bin/env python3
"""使用已导入工程的 Unity 引用和分析器，编译全部热更源码；不启动编辑器。"""
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def main():
    """从 Unity 生成的工程读取真实平台符号和引用，输出到临时目录。"""
    repository = Path(__file__).resolve().parent.parent
    project = repository / "Client/GameTopRouge"
    namespace = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
    document = ET.parse(project / "Hotfix.csproj").getroot()
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise SystemExit("需要安装 .NET SDK。")
    sdk = subprocess.check_output([dotnet, "--list-sdks"], text=True).strip().splitlines()[-1]
    version, directory = re.fullmatch(r"(\S+) \[(.+)\]", sdk).groups()
    compiler = Path(directory) / version / "Roslyn/bincore/csc.dll"
    references = []
    for reference in document.findall(".//m:Reference", namespace):
        hint = reference.find("m:HintPath", namespace)
        if hint is not None:
            path = Path(hint.text.replace("\\", "/"))
            references.append(path if path.is_absolute() else project / path)
    for reference in document.findall(".//m:ProjectReference", namespace):
        name = Path(reference.attrib["Include"].replace("\\", "/")).stem
        references.append(project / "Library/ScriptAssemblies" / (name + ".dll"))
    missing = [str(path) for path in references if not path.exists()]
    if missing:
        raise SystemExit("请先用对应 Unity/团结引擎导入工程，以下引用缺失：\n" + "\n".join(missing))
    with tempfile.TemporaryDirectory(prefix="gametop_hotfix_compile_") as temporary:
        output = Path(temporary)
        arguments = ["-nostdlib+", "-target:library", "-unsafe+", "-langversion:9.0", "-out:" + str(output / "Hotfix.dll")]
        arguments.append("-define:" + document.find(".//m:DefineConstants", namespace).text)
        arguments.extend("-r:" + str(path) for path in references)
        arguments.extend("-analyzer:" + element.attrib["Include"] for element in document.findall(".//m:Analyzer", namespace))
        sources = sorted((project / "Assets/AAAGame/ScriptsHotfix").rglob("*.cs"))
        arguments.extend(map(str, sources))
        response = output / "compile.rsp"
        response.write_text("\n".join('"' + argument + '"' for argument in arguments), encoding="utf-8")
        print(f"编译 {len(sources)} 个热更源文件，使用现有 Unity 引用。", flush=True)
        subprocess.run([dotnet, str(compiler), "-noconfig", "@" + str(response)], cwd=repository, check=True)


if __name__ == "__main__":
    main()
