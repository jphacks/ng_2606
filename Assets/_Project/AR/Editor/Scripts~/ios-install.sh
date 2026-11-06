#!/bin/zsh
# つないだ iPhone に、アプリ（Main シーン）をビルドして入れ、起動する。
# どの iPhone でも使える（この Mac につないだ物の中から選ぶ）。手順は、同じフォルダの README.md。
#
#   ./ios-install.sh                 つないだ iPhone を選んで、ビルドから入れる
#   ./ios-install.sh --list          つないだ iPhone の一覧を出すだけ
#   ./ios-install.sh --skip-unity    Unity のビルドを飛ばす（前に書き出した Xcode のプロジェクトを使う。別の iPhone に入れるときに速い）
#   ./ios-install.sh --device 名前   名前（の一部）か UDID で、iPhone を選ぶ
#
# 署名のチーム ID は、環境変数 TEAM で変えられる（決めていなければ、これまでのチーム）。
set -u
TEAM="${TEAM:-2J4S74NK58}"
BUNDLE_ID="jp.jphacks.ng2606.deskstagear"
UNITY="/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")/../../../../.." && pwd)"
XCODE_PROJECT="$HOME/Builds/DeskStageAR/Unity-iPhone.xcodeproj"
WORK="${TMPDIR:-/tmp}/deskstage-ios"
mkdir -p "$WORK"

LIST_ONLY=0; SKIP_UNITY=0; WANT=""
while [ $# -gt 0 ]; do
  case "$1" in
    --list) LIST_ONLY=1 ;;
    --skip-unity) SKIP_UNITY=1 ;;
    --device) shift; WANT="${1:-}" ;;
    *) echo "知らない指定です：$1"; exit 2 ;;
  esac
  shift
done

# つないだ iPhone（本物だけ）を、「UDID<タブ>名前<タブ>状態」で並べる
xcrun devicectl list devices --json-output "$WORK/devices.json" >/dev/null 2>&1 || { echo "iPhone の一覧を取れませんでした（Xcode は入っていますか）"; exit 1; }
DEVICES=$(/usr/bin/python3 - "$WORK/devices.json" <<'PY'
import json, sys
for d in json.load(open(sys.argv[1]))["result"]["devices"]:
    hw = d.get("hardwareProperties", {})
    # まだペアリングしていない iPhone は、reality が空になる。シミュレータ（simulated）だけを除く
    if hw.get("reality") == "simulated" or hw.get("platform") != "iOS":
        continue
    name = d.get("deviceProperties", {}).get("name", "?")
    conn = d.get("connectionProperties", {})
    state = "未ペアリング" if conn.get("pairingState") == "unpaired" else conn.get("tunnelState", "?")
    print(f"{hw.get('udid')}\t{name}\t{state}")
PY
)
if [ -z "$DEVICES" ]; then
  echo "つないだ iPhone が見つかりません。ケーブルでつなぎ、iPhone で「このコンピュータを信頼」を押してください"
  exit 1
fi

echo "つないだ iPhone："
echo "$DEVICES" | awk -F'\t' '{ printf "  %d. %s（%s）\n", NR, $2, $3 }'
[ $LIST_ONLY -eq 1 ] && exit 0

# 使う iPhone を決める：--device の指定 → 1 台だけならそれ → 番号で選ぶ
if [ -n "$WANT" ]; then
  LINE=$(echo "$DEVICES" | awk -F'\t' -v w="$WANT" 'index($1, w) || index($2, w)' | head -1)
elif [ $(echo "$DEVICES" | wc -l) -eq 1 ]; then
  LINE="$DEVICES"
else
  printf "入れる iPhone の番号："
  read -r N
  LINE=$(echo "$DEVICES" | sed -n "${N}p")
fi
if [ -z "${LINE:-}" ]; then
  echo "その iPhone は、見つかりません"
  exit 1
fi
UDID=$(echo "$LINE" | cut -f1)
NAME=$(echo "$LINE" | cut -f2)
STATE=$(echo "$LINE" | cut -f3)
echo "→ 「$NAME」に入れます"

# まだペアリングしていない iPhone は、ここでペアリングする（iPhone に「信頼しますか？」が出るので、「信頼」を押してパスコードを入れる）
if [ "$STATE" = "未ペアリング" ]; then
  echo "[0/4] ペアリングします。iPhone のロックを外し、「このコンピュータを信頼しますか？」で「信頼」を押してください"
  xcrun devicectl manage pair --device "$UDID" > "$WORK/pair.log" 2>&1 \
    || { echo "ペアリングできませんでした。iPhone で「信頼」を押してから、もう一度実行してください。ログの最後："; tail -5 "$WORK/pair.log"; exit 1; }
fi

# 1. Unity：Main シーンを、Xcode のプロジェクトに書き出す（Unity のエディタを閉じておく）
if [ $SKIP_UNITY -eq 0 ]; then
  echo "[1/4] Unity でビルドしています（数分かかります）"
  "$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod DeskStage.AR.EditorTools.BuildIos.RunMain -logFile "$WORK/unity-build.log" -quit \
    || { echo "Unity のビルドに失敗しました。ログ：$WORK/unity-build.log"; exit 1; }
else
  echo "[1/4] Unity のビルドは飛ばします"
fi
[ -d "$XCODE_PROJECT" ] || { echo "Xcode のプロジェクトがありません。--skip-unity を外して、もう一度"; exit 1; }

# 2. Xcode：この iPhone を宛先にして署名する（初めての iPhone は、ここでチームに登録される）
echo "[2/4] Xcode でビルドしています（数分かかります）"
xcodebuild -project "$XCODE_PROJECT" -scheme Unity-iPhone -configuration Release \
  -destination "id=$UDID" -derivedDataPath "$WORK/dd" \
  DEVELOPMENT_TEAM="$TEAM" CODE_SIGN_STYLE=Automatic PROVISIONING_PROFILE_SPECIFIER= PROVISIONING_PROFILE= \
  -allowProvisioningUpdates -allowProvisioningDeviceRegistration build > "$WORK/xcode-build.log" 2>&1
if [ $? -ne 0 ]; then
  if grep -q "Developer Mode disabled" "$WORK/xcode-build.log"; then
    echo "この iPhone は、デベロッパモードがオフです。設定 → プライバシーとセキュリティ → デベロッパモード をオンにして（再起動します）、もう一度実行してください"
  else
    echo "Xcode のビルドに失敗しました。ログの最後："; tail -20 "$WORK/xcode-build.log"
  fi
  exit 1
fi
APP=$(ls -d "$WORK"/dd/Build/Products/Release-iphoneos/*.app | head -1)

# 3. 入れる
echo "[3/4] iPhone に入れています"
xcrun devicectl device install app --device "$UDID" "$APP" > "$WORK/install.log" 2>&1 \
  || { echo "入れられませんでした。ログの最後："; tail -10 "$WORK/install.log"; exit 1; }

# 4. 起動する
echo "[4/4] 起動しています"
if ! xcrun devicectl device process launch --device "$UDID" --terminate-existing "$BUNDLE_ID" > "$WORK/launch.log" 2>&1; then
  if grep -q "Locked" "$WORK/launch.log"; then
    echo "入れました。iPhone の画面のロックを外して、アプリを開いてください"
  elif grep -q "not been explicitly trusted\|invalid code signature" "$WORK/launch.log"; then
    echo "入れました。初めての iPhone は、設定 → 一般 → VPN とデバイス管理 で、開発元を「信頼」してから、アプリを開いてください"
  else
    echo "入れましたが、起動できませんでした。ログ：$WORK/launch.log"
  fi
  exit 0
fi
echo "「$NAME」で起動しました"
