#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
command -v python3 >/dev/null || { echo "Python 3 required"; exit 1; }
python3 -m py_compile linux/pushadb.py
rm -rf dist-linux/PushADB-Linux
mkdir -p dist-linux/PushADB-Linux
cp linux/pushadb.py dist-linux/PushADB-Linux/
cp README.md dist-linux/PushADB-Linux/
cat > dist-linux/PushADB-Linux/start.sh <<'EOF'
#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
exec python3 ./pushadb.py
EOF
chmod +x dist-linux/PushADB-Linux/start.sh
tar -C dist-linux -czf PushADB-Linux-Preview.tar.gz PushADB-Linux
sha256sum PushADB-Linux-Preview.tar.gz > PushADB-Linux-Preview.tar.gz.sha256
tar -tzf PushADB-Linux-Preview.tar.gz | grep -q "PushADB-Linux/start.sh" || { echo "Archive validation failed"; exit 1; }
echo "Verified PushADB-Linux-Preview.tar.gz"
