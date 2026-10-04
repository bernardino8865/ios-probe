"""Confere o alinhamento (p_align dos segmentos LOAD) de TODAS as bibliotecas nativas .so de um APK/AAB.
A Google Play exige 16 KB (0x4000) para apps que miram Android 15+ (e bloqueia atualizações sem isso).
Uso: python tools/check_elf_alignment.py <arquivo.aab|.apk>      (código de saída 1 se alguma biblioteca de 64 bits não estiver alinhada)
"""
import struct
import sys
import zipfile


def load_aligns(data: bytes):
    if data[:4] != b"\x7fELF" or data[4] != 2:      # só ELF de 64 bits
        return None
    phoff = struct.unpack_from("<Q", data, 0x20)[0]
    entry_size, count = struct.unpack_from("<HH", data, 0x36)
    return sorted({struct.unpack_from("<Q", data, phoff + i * entry_size + 0x30)[0]
                   for i in range(count) if struct.unpack_from("<I", data, phoff + i * entry_size)[0] == 1})


def main(path: str) -> int:
    bad, total = [], 0
    with zipfile.ZipFile(path) as archive:
        for info in sorted(archive.infolist(), key=lambda i: i.filename):
            if not info.filename.endswith(".so"):
                continue
            aligns = load_aligns(archive.open(info).read(16384))
            if aligns is None:
                print(f"  (ignorado, não é ELF 64) {info.filename}")
                continue
            total += 1
            ok = all(a >= 0x4000 for a in aligns)
            print(f"  {'OK    ' if ok else 'FALHOU'} {info.filename}  p_align={[hex(a) for a in aligns]}")
            if not ok:
                bad.append(info.filename)
        # o próprio zip: bibliotecas guardadas SEM compressão e alinhadas (necessário para 16 KB em APK; informativo em AAB)
    print(f"\n{total} biblioteca(s) .so de 64 bits; {len(bad)} sem alinhamento de 16 KB")
    for name in bad:
        print(f"  NÃO ALINHADA: {name}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
