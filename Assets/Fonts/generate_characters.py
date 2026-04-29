import unicodedata

chars = set()

# ひらがな
for i in range(0x3041, 0x3097):
    chars.add(chr(i))

# カタカナ
for i in range(0x30A1, 0x30FB):
    chars.add(chr(i))

# 常用漢字（JIS第一水準・第二水準の範囲）
for i in range(0x4E00, 0x9FFF):
    c = chr(i)
    if unicodedata.name(c, '').startswith('CJK'):
        chars.add(c)

# 英数字
for c in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789':
    chars.add(c)

# 記号
for c in '、。！？「」『』…・ー〜♡★☆♪→©®™#@&% ':
    chars.add(c)

result = ''.join(sorted(chars))
with open('Assets/Fonts/japanese_characters.txt', 'w', encoding='utf-8') as f:
    f.write(result)

print(f'文字数: {len(result)}')
