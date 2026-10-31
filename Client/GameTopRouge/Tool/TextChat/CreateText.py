with open('gb2312_chars.txt', 'w', encoding='utf-8') as f:
    for code in range(0x4E00, 0x9FA5+1):
        try:
            char = chr(code)
            if '\u4e00' <= char <= '\u9fff':  # 中文字符范围校验
                f.write(char)
        except:
            continue