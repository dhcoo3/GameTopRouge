#!/usr/bin/env python3
"""调用现有协议导出器，并从 proto 源注释生成两端可重复维护的中文 XML 注释。"""
import html
import json
import re
import subprocess
from pathlib import Path


def protocol_comments(source):
    """读取消息和字段前的中文说明，缺少说明的旧协议提供明确的协议名称。"""
    descriptions, fields = {}, {}
    for path in source.rglob("*.proto"):
        pending, message = [], None
        for line in path.read_text(encoding="utf-8-sig").splitlines():
            stripped = line.strip()
            if stripped.startswith("//"):
                pending.append(stripped[2:].strip())
                continue
            match = re.match(r"message\s+(\w+)", stripped)
            field = re.match(r"(?:repeated\s+)?[\w.]+\s+(\w+)\s*=", stripped)
            if match:
                message = match.group(1)
                descriptions[message] = " ".join(pending) or f"网络协议消息 {message}。"
            elif field and message:
                fields[(message, field.group(1))] = " ".join(pending) or f"协议字段 {field.group(1)}。"
            elif stripped == "}":
                message = None
            pending = []
    return descriptions, fields


def annotate(path, descriptions, fields):
    """为生成声明补齐注释；属性标记始终放在 XML 文档之后。"""
    output, attributes, owner = [], [], ""
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        stripped = line.strip()
        if stripped.startswith("["):
            attributes.append(line)
            continue
        docs = []
        indent = line[: len(line) - len(line.lstrip())]
        declaration = re.search(r"public\s+(?:static\s+|partial\s+)*(?:class|struct)\s+(\w+)", line)
        prop = re.search(r"public\s+[\w<>.,]+\s+(\w+)\s*\{\s*get;", line)
        constant = re.search(r"public\s+const\s+\w+\s+(\w+)\s*=", line)
        method = re.search(r"public\s+(?:static\s+)?(?:async\s+)?[\w<>.,]+\s+(\w+)\((.*)\)", line)
        if declaration:
            owner = declaration.group(1)
            docs = [descriptions.get(owner, f"网络协议辅助定义 {owner}。")]
        elif prop:
            name = prop.group(1)
            docs = [fields.get((owner, name), {"ErrorCode": "操作结果，零表示成功。", "ResponseType": "请求对应的响应类型，仅供框架推断。"}.get(name, f"协议字段 {name}。"))]
        elif constant:
            docs = [f"{constant.group(1)} 的协议编号。"]
        elif method:
            name, parameters = method.groups()
            docs = [{"Create": "从消息池创建协议对象。", "Return": "归还当前协议对象。", "Dispose": "清空字段并归还消息池。", "CopyTo": "复制协议字段到目标消息。", "DeepCopy": "创建独立的消息副本。", "OpCode": "取得本消息的协议编号。"}.get(name, f"发送 {name} 协议请求并等待响应。")]
        if docs:
            output.append(indent + "/// <summary>" + html.escape(docs[0], quote=False) + "</summary>")
            if method and not declaration:
                for parameter in method.group(2).split(","):
                    if not parameter.strip():
                        continue
                    name = parameter.split("=")[0].strip().split()[-1]
                    detail = {"autoReturn": "是否允许对象自动归还消息池。", "other": "接收副本字段的目标消息。", "session": "已经建立的网络会话。"}.get(name, f"协议参数 {name}。")
                    output.append(indent + f'/// <param name="{name}">{detail}</param>')
        output.extend(attributes)
        attributes = []
        output.append(line)
    output.extend(attributes)
    path.write_text("\n".join(output) + "\n", encoding="utf-8")


def main():
    """按工具配置运行导出，并为客户端与服务端生成结果同步补充文档。"""
    tool = Path(__file__).resolve().parent
    subprocess.run(["dotnet", str(tool / "Fantasy.ProtocolExportTool.dll"), "export", "--silent"], cwd=tool, check=True)
    settings = json.loads((tool / "ExporterSettings.json").read_text(encoding="utf-8-sig"))["Export"]
    source = (tool / settings["NetworkProtocolDirectory"]["Value"]).resolve()
    descriptions, fields = protocol_comments(source)
    for key in ("NetworkProtocolServerDirectory", "NetworkProtocolClientDirectory"):
        destination = (tool / settings[key]["Value"]).resolve()
        for path in destination.glob("*.cs"):
            annotate(path, descriptions, fields)


if __name__ == "__main__":
    main()
