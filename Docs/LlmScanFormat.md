# 写真の読み取り：答えの形と指示文

B の Step 6 で作った、Claude への指示文と答えの JSON の形です（00_overview.md 9.4）。A のサーバー（A Step 24）と B の StageReader（B Step 7）が、同じファイルを使います。

| ファイル | 中身 | 使う人 |
| --- | --- | --- |
| Assets/_Project/Stage/Llm/scan_prompt.md | Claude への指示文 | サーバー（system か、写真と一緒の文として渡す） |
| Assets/_Project/Stage/Llm/scan_result.schema.json | 答えの JSON スキーマ | サーバー（構造化出力 output_config.format に渡す）、B（形の確認） |

## 答えの項目

| 項目 | 型 | 単位 | 意味 | 範囲（B が確かめる） |
| --- | --- | --- | --- | --- |
| paperFound | true / false | ― | 基準の紙が写っていて、長さの基準に使えたか | false なら箱を作らない |
| desk | 幅と奥行き、または null | cm | 机の天面の大きさ。机の端が写っていなければ null | 20〜300 |
| blocks | 物の一覧 | ― | 机の上の物。紙は入れない | 0〜100件 |
| notes | 文字列 | ― | 人が読むメモ | ― |

blocks の1件：

| 項目 | 単位 | 意味 | 範囲（B が確かめる） |
| --- | --- | --- | --- |
| label | ― | 何の物か | ― |
| centerXCm、centerZCm | cm | 上から見た中心。紙の左手前の角から右（X）と奥（Z）へ | 机の中 |
| widthCm、depthCm | cm | 上から見た長い方の辺、短い方の辺 | 1〜60 |
| heightCm | cm | 机の天面からこの物の上面まで（積んだ物は机から） | 0.5〜40 |
| yawDegrees | 度 | 長い方の辺が +X から反時計回りに何度か | 0 以上 180 未満 |
| confidence | ― | 自信の度合い | 0〜1 |

構造化出力のスキーマには、数の範囲（minimum など）を書けません。範囲は説明文に書き、B の Step 7 で確かめて、外れた物は箱にしません。

## ステージ座標への直し方（B Step 7）

- cm を m にする（100 で割る）
- 机の中心が原点になるように、`x = centerXCm/100 − 机の幅/2`、`z = centerZCm/100 − 机の奥行き/2` にする
- 箱の大きさ：`size = [widthCm, heightCm, depthCm] / 100`、中心の高さは `heightCm/100/2`
- 回転：答えは「上から見て反時計回り」。Unity の `Quaternion.Euler(0, θ, 0)` は上から見て時計回りなので、`θ = (360 − yawDegrees) % 360`
- desk が null なら、物を囲む長方形に 10cm の余白を付けた範囲を机にする
- 机の奥行きの方が長いときは、ステージの長い辺を X にする決まり（00_overview.md 3.1）に合わせて 90度回す（B Step 7 で決める）

## 試し方（Step 6 の4）

### 並べ方

机（90cm × 44cm）に、次のとおり置きます。A4 の紙と重ならないように、お手本ステージ（Docs/SampleLayout.md）の箱を右にずらしています。

| 物 | 置き方 | 左の辺からの距離 | 手前の辺からの距離 |
| --- | --- | --- | --- |
| A4 の紙 | 縦長。短い辺を手前に | 0〜21 | 0〜29.7 |
| 箱A（2cm、15 × 22cm） | 22cm の辺を前後に | 25〜40 | 11〜33 |
| 箱B（3cm、10 × 35cm） | 35cm の辺を前後に。箱A の右にぴったり | 40〜50 | 4.5〜39.5 |
| 箱C（14cm、22 × 20cm） | 22cm の辺を左右に | 60〜82 | 12〜32 |

### 写真

1. 真上から1枚。机の全体と紙が入るように
2. 手前から斜め（高さ 30〜40cm くらい、机を見下ろす向き）から1枚。箱の側面が見えるように

### claude.ai に渡す文

claude.ai の新しいチャットに、2枚の写真（真上を先に）と、scan_prompt.md の全文を貼ります。claude.ai では JSON の形を固定できないので、最後に次の文を足します。

```
答えは、次の形の JSON だけにしてください。

{"paperFound": true, "desk": {"widthCm": 0, "depthCm": 0}, "blocks": [{"label": "", "centerXCm": 0, "centerZCm": 0, "widthCm": 0, "depthCm": 0, "heightCm": 0, "yawDegrees": 0, "confidence": 0}], "notes": ""}
```

### 正しい答え（比べる相手）

| 物 | centerXCm | centerZCm | widthCm | depthCm | heightCm | yawDegrees |
| --- | --- | --- | --- | --- | --- | --- |
| 箱A | 32.5 | 22 | 22 | 15 | 2 | 90 |
| 箱B | 45 | 22 | 35 | 10 | 3 | 90 |
| 箱C | 71 | 22 | 22 | 20 | 14 | 0 |
| 机 | ― | ― | 90 | 44 | ― | ― |

### 合格の目安（仮）

- 3つの箱がすべて答えに入っている。紙は入っていない
- 高さの差：±1cm 以内（箱A と箱B の 1cm の差が分かるか）
- 位置と大きさの差：±2cm 以内

## 試した結果

（まだ）
