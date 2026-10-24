# stage.json version 2 の説明

ステージ1つ分のデータのファイルです。中身は 00_overview.md の 4.2（StageData）と同じ形です。B が保存と読み込みを行い、A・C・D は StageContext.CurrentStage から同じ中身を読みます。

## 決まり

- 長さはメートル、角度は度。Unity の1単位＝1m
- 座標はステージ座標（00_overview.md の 3.1）。原点は机の天面の長方形の中心、高さ0＝机の天面、Y が真上、長い方の辺が X
- 位置や大きさは「長さ3の数値の配列」[x, y, z] で書く
- Unity 標準の JsonUtility で読み書きする
- 下の「範囲」を外れる値があるファイルは、読み込むときに読み込めないものとして扱う（B の Step 15 で表示と復旧を作る）

## 置き場所

| 置き場所 | 中身 |
| --- | --- |
| Assets/StreamingAssets/Stages/<ステージID>/stage.json | ビルドに含めるお手本ステージ。iPhone では読むだけ |
| Assets/StreamingAssets/Stages/index.json | 同梱するステージIDの一覧（iPhone では同梱フォルダの中身を一覧できないため） |
| Application.persistentDataPath の Stages/<ステージID>/stage.json | 端末に保存したステージ。初回起動時に同梱分をここへコピーし、以降はここだけを読み書きする |

表面の画像（任意）は、stage.json と同じフォルダに置きます。

index.json は、同梱するステージの id と更新番号（revision）の一覧です。同梱のステージを直したら revision を1つ上げます（端末に古い版が残っているときに上書きするため。B の Step 10）。

```json
{
  "stages": [
    { "id": "sample-01", "revision": 1 }
  ]
}
```

## 項目

| 項目 | 型 | 単位 | 意味 | 範囲 |
| --- | --- | --- | --- | --- |
| version | 整数 | ― | ファイルの形の版 | 2 だけ。1 は「読み込めません（古い形式）」と表示する |
| id | 文字列 | ― | ステージID。フォルダ名と同じ | 空でない |
| name | 文字列 | ― | 一覧に出す表示名 | ― |
| createdAt | 文字列 | ― | スキャンした日時（ISO 8601、例：2026-10-23T14:05:00+09:00） | ― |
| desk | 幅と奥行き | m | 机の天面の長方形の大きさ。中心は原点 | 幅・奥行きとも 0.2〜3 |
| boxes | BoxData の一覧 | ― | ステージの箱（下の表） | 0〜100個 |
| spawn | 位置（長さ3）と向き（yawDegrees） | m、度 | キャラのスタート地点と向き | ― |
| goal | 位置（長さ3）と半径 | m | ゴール | 半径 0.01〜0.05 |
| checkpoints | 位置と半径の一覧 | m | チェックポイント | 0〜5個。半径 0.01〜0.05 |
| fallDepth | 数値 | m | 机の天面からどれだけ下に落ちたら落下とするか。初期値 0.10 | 0より大きい |

### BoxData（箱1つ）

| 項目 | 型 | 単位 | 意味 | 範囲 |
| --- | --- | --- | --- | --- |
| id | 文字列 | ― | 箱の番号（例：box-01） | ステージの中で重ならない |
| center | 長さ3 | m | 箱の中心。高さ（y）は箱の高さの半分 | y は size の高さの半分（1mm までの差は許す） |
| size | 長さ3 | m | 幅・高さ・奥行き。高さは上面から机の天面まで | 幅・奥行き 0.01〜0.6、高さ 0.005〜0.4 |
| yawDegrees | 数値 | 度 | 上から見た回転（Y軸まわりだけ） | 0以上360未満 |
| source | 文字列 | ― | auto：スキャンで作った。manual：補助で足した・直した | auto か manual |
| texture | 文字列 | ― | 表面の画像のファイル名。なければ空 | ― |

箱はすべて机の天面（高さ0）から立っています。宙に浮いた箱や、箱の下のすき間は表せません。

## 範囲の一覧（読み込み時の確認に使う）

| 項目 | 範囲 |
| --- | --- |
| desk の幅・奥行き | 0.2〜3m |
| boxes の数 | 0〜100個 |
| 箱の幅・奥行き | 0.01〜0.6m |
| 箱の高さ | 0.005〜0.4m |
| 箱の中心の高さ | 箱の高さの半分（1mmまでの差は許す） |
| yawDegrees | 0以上360未満 |
| goal と checkpoints の半径 | 0.01〜0.05m |
| checkpoints の数 | 0〜5個 |
| fallDepth | 0より大きい |

## 中の項目の名前

共通コード StageData（Assets/_Project/Shared/StageData.cs）の名前と同じです。

| 項目 | 中の名前 |
| --- | --- |
| desk | width（幅）、depth（奥行き） |
| spawn | position（長さ3）、yawDegrees |
| goal、checkpoints の1つ | position（長さ3）、radius |

## 例

箱2つのステージです。

```json
{
  "version": 2,
  "id": "sample-01",
  "name": "おてほん",
  "createdAt": "2026-10-23T14:05:00+09:00",
  "desk": { "width": 1.2, "depth": 0.6 },
  "boxes": [
    {
      "id": "box-01",
      "center": [-0.2, 0.05, 0.0],
      "size": [0.2, 0.1, 0.15],
      "yawDegrees": 0,
      "source": "auto",
      "texture": ""
    },
    {
      "id": "box-02",
      "center": [0.1, 0.02, 0.05],
      "size": [0.1, 0.04, 0.1],
      "yawDegrees": 30,
      "source": "manual",
      "texture": ""
    }
  ],
  "spawn": { "position": [-0.45, 0.0, 0.0], "yawDegrees": 90 },
  "goal": { "position": [-0.2, 0.1, 0.0], "radius": 0.03 },
  "checkpoints": [
    { "position": [0.1, 0.04, 0.05], "radius": 0.03 }
  ],
  "fallDepth": 0.1
}
```
