# iPhone にアプリを入れる（この Mac から、どの iPhone にでも）

`ios-install.sh` は、この Mac につないだ iPhone を選んで、アプリをビルドして入れ、起動します。
このフォルダ（名前の最後が `~`）は、Unity に読み込まれません。

## 初めての iPhone で、1 回だけやること

1. iPhone を、ケーブルでこの Mac につなぐ。iPhone に出る「このコンピュータを信頼しますか？」で「信頼」を押す
2. iPhone の **デベロッパモード** をオンにする：設定 → プライバシーとセキュリティ → デベロッパモード（オンにすると再起動します）
   - 項目が無いときは、一度 Xcode を開いて、iPhone をつないだままにすると出てきます
3. 下の「入れ方」でアプリを入れたあと、iPhone の 設定 → 一般 → VPN とデバイス管理 で、開発元を「信頼」する

署名のチームが無料の Apple ID（個人のチーム）の場合、入れられる iPhone は 3 台まで、入れたアプリは 7 日で開けなくなります（入れ直せば、また使えます）。

## 入れ方

Unity のエディタは閉じておく（開いたままだと、Unity のビルドが動きません）。

```sh
cd Assets/_Project/AR/Editor/Scripts~
./ios-install.sh                 # つないだ iPhone を選んで、ビルドから入れる
./ios-install.sh --skip-unity    # 2 台目からは、Unity のビルドを飛ばせる（速い）
./ios-install.sh --list          # つないだ iPhone の一覧
./ios-install.sh --device 名前   # 名前の一部か UDID で選ぶ
```

- 何台もつないでいるときは、番号を聞かれます。1 台だけなら、その iPhone に入れます
- 初めての iPhone は、Xcode のビルドのときに、自動でチームに登録されます
- 署名のチームを変えるときは、`TEAM=チームID ./ios-install.sh`

## 撮影（サーバー）を使うには

- アプリが写真を送る先は、`Assets/_Project/AR/Resources/ServerConfig.asset` の `serverUrl` です（Git には入れていません）。ビルドしたときの値が、アプリに入ります
- いまは `http://<この Mac の名前>.local:8000` にしています。この名前は、同じ Wi-Fi（または同じ iPhone のテザリング）にいれば、どの iPhone からも届きます
- iPhone も、この Mac と同じ Wi-Fi（またはテザリング）につないでください。会場の Wi-Fi は、端末どうしの通信を止めていることがあります。そのときは、iPhone のテザリングに Mac をつなぎます
- サーバーは、この Mac で動かしておきます（起動のしかたは `Server/README.md`）

## うまくいかないとき

| 出る文 | すること |
|---|---|
| つないだ iPhone が見つかりません | ケーブルを差し直し、iPhone で「信頼」を押す |
| Xcode のビルドに失敗しました | ログの最後を見る。署名の失敗なら、Xcode を開いて、Settings → Accounts に Apple ID が入っているか確かめる |
| ロックを外して、アプリを開いてください | iPhone の画面のロックを外して、アプリを開く |
| 開発元を「信頼」してから | 設定 → 一般 → VPN とデバイス管理 で「信頼」 |
