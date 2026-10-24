# コースの配置（CoursePlacer）

担当：C（C_gameplay.md の Step 12）。D のたしかめ画面が `UiRequests.RequestPlaceCourseItem(物, タップ位置)` を出すと、C がステージデータ（spawn、goal、checkpoints）を書き換え、目印を出し直して、`GameEvents.CoursePlaced(物, 結果, 理由)` を返します。保存は B が行います。

## 置き方の決まり

| 物 | 決まり |
| --- | --- |
| スタート | 1つだけ。もう一度置くと動く。向きは、置いたときのスマホの水平な向き（置いた子どもから見て奥） |
| ゴール | 1つだけ。もう一度置くと動く。触れる範囲は半径 3cm |
| チェックポイント | 最大5個。既にあるチェックポイントの 3cm 以内をタップすると、それを消す。触れる範囲は半径 3cm |

- 相手は Terrain（B の箱と机の板）だけ。上向きの面（傾き約30度まで）にだけ置ける
- 置いた点は、面から 1mm 上

## CoursePlaced の結果と理由（D の方へ）

理由は、D が子ども向けの文に置き換えるための決まった記号です。文は Docs/Wording.md に足してください。

| 結果 | 理由の記号 | いつ | 文の例（案） |
| --- | --- | --- | --- |
| Placed | （空） | 置けた | ― |
| Placed | `checkpoint-removed` | チェックポイントを消した | ― |
| Rejected | `not-on-terrain` | 机か箱の上ではない所をタップした | つくえか ものの うえを タップしてね |
| Rejected | `not-top` | 箱の側面をタップした | ものの うえを タップしてね |
| Rejected | `checkpoint-full` | チェックポイントがもう5個ある | チェックポイントは 5こ までだよ |
| MayBeUnreachable | `may-be-unreachable` | スタートからゴールへ、壁を通らないと行けないかもしれない（置くのは止めない） | ？の印と、Wording の check.wallHint |

`may-be-unreachable` を返す判定（行けるかの調べ方）は、作り方を決めてから入れます（Step 12 の 7）。
