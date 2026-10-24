# 画面の文言表

画面に出る文はすべてこの表にまとめる。UI の Wording アセットは、この表と同じ内容にする（画面は Wording から文を読む）。
対象の年齢は【未決】。決まるまでは小学校低学年として、ひらがな中心で書く。年齢が決まったら、この表だけを直す。

担当：D。キーは Wording アセットで使う名前（英字、画面名.内容）。表を直したら、Unity のメニュー「DeskStage/D/文言の表を読み込む」で Wording アセットに読み込む。

## タイトル

| キー | 文 | 使う場面 |
| --- | --- | --- |
| title.start | はじめる | タイトルのボタン |

## ステージ一覧

| キー | 文 | 使う場面 |
| --- | --- | --- |
| list.play | あそぶ | ステージのボタン |
| list.delete | けす | ステージのボタン |
| list.deleteConfirm | ほんとうに けす？ | けすを押したときの確かめ |
| list.new | あたらしくつくる | 一覧のボタン |
| list.bestTime | さいそく | 最速記録の見出し |
| list.unreadable | よみこめません | 読み込めないステージ（灰色） |
| list.unreadableOld | よみこめません（ふるい かたち） | version 1 のステージ |

## ならべかたのコツ

| キー | 文 | 使う場面 |
| --- | --- | --- |
| tips.flat | うえが たいらな ものを ならべよう | 1枚目（段ボール箱、積み木、本の絵） |
| tips.height | キャラは このくらい とべるよ | 2枚目（たかさものさしの絵） |
| tips.stairs | たかい ところには、ひくい ものを となりに おこう | 3枚目（階段のように並べた絵） |
| tips.scan | スキャンする | 右下のボタン |
| tips.beforeScan | ものを ならべたら おしてね | ボタンの上（物を並べずに始めるのを防ぐ） |
| tips.planB | てのひらより おおきい ものが いいよ | 案B を選んだ場合に足す絵 |
| tips.planC | もようシートを うえに のせよう | 案C を選んだ場合に足す絵 |

## スキャン（案内 GuidanceView）

| キー | 文 | 使う場面 |
| --- | --- | --- |
| guide.starting | じゅんび しているよ | Starting |
| guide.findingDesk | つくえを うつしてね | FindingDesk |
| guide.findingDeskLong | つくえの うえを ゆっくり うつしてね | FindingDesk で10秒たった |
| guide.deskFound | つくえを タップしてね | FindingDesk で机の面が見つかった |
| guide.scanning | つくえの まわりを ゆっくり あるいてね | Scanning |
| guide.limitedDark | へやを あかるく してね | Limited、理由 Dark |
| guide.limitedFast | もっと ゆっくり うごかしてね | Limited、理由 FastMotion |
| guide.limitedFew | つくえの ほうに むけてね | Limited、理由 FewFeatures |
| guide.lost | もういちど つくえを うつしてね | Lost |

## スキャン画面

| キー | 文 | 使う場面 |
| --- | --- | --- |
| scan.boxCount | みつけた はこ：{0} | 画面の上。{0} は箱の数 |
| scan.done | できた | 右下のボタン |

## たしかめ画面

| キー | 文 | 使う場面 |
| --- | --- | --- |
| check.question | ほんものと おなじかな？ | 上の中央 |
| check.rescan | もういちどスキャン | 左上のボタン |
| check.rescanConfirm | はこを ぜんぶ つくりなおすよ。いい？ | もういちどスキャンの確かめ |
| check.wallHint | とどかないよ。ひくい ものを となりに おいて、もういちど スキャンしてね | Wall の箱のそば（小さく） |
| check.fix | なおす | 左下のボタン（補助の開閉） |
| check.addBox | ＋はこ | 補助 |
| check.addBoxHint | おきたい ところを タップしてね | ＋はこ を押した後 |
| check.delete | けす | 補助（選んだ箱の横） |
| check.undo | もどす | 補助 |
| check.pickAround | まわりこんで えらんでね | 手前の箱が選ばれてしまうとき |
| check.start | スタート | 切り替えボタン |
| check.goal | ゴール | 切り替えボタン |
| check.checkpoint | チェックポイント | 切り替えボタン |
| check.save | ほぞん | 右下のボタン |
| check.saveName | つくえ 1 | 名前を決める画面の初期値 |
| check.saveOk | ほぞん したよ | SaveCompleted が成功 |
| check.saveNg | ほぞん できなかったよ | SaveCompleted が失敗 |
| check.play | あそぶ | 保存できたあとの大きなボタン |
| check.unsavedConfirm | ほぞん していないよ。もどる？ | 保存せずに戻ろうとしたとき |
| check.mark.jump | とぶ | 段の印（緑）に付ける文字 |
| check.mark.wall | ばつ | 段の印（赤）に付ける文字 |
| check.maybeUnreachable | ？ | CoursePlaced が「とどかないかも」のゴールのそば |

## プレイ画面

| キー | 文 | 使う場面 |
| --- | --- | --- |
| play.jump | ジャンプ | 右下のジャンプボタン |
| play.pause | いちじていし | 右上のボタン（絵と一緒に） |
| play.resume | つづける | 一時停止メニュー |
| play.retry | もういちど | 一時停止メニュー |
| play.toList | ステージいちらんへ | 一時停止メニュー |
| play.checkpoint | チェックポイント！ | CheckpointPassed（1秒） |
| play.fell | おちちゃった | Fell（1秒） |
| play.hot | スマホが あつく なったよ。すこし やすもう | ThermalStateChanged が「危険」 |

## クリア画面

| キー | 文 | 使う場面 |
| --- | --- | --- |
| clear.newRecord | しんきろく！ | 最速記録のとき |
| clear.retry | もういちど | ボタン |
| clear.rebuild | ならべなおす | ボタン |
| clear.toList | ステージいちらんへ | ボタン |

## 置き場のない文（手順書で未決のもの）

- 置けない理由の文（CoursePlaced の「置けない」）は、C が返す理由に合わせて Step 13 で足す。
- 矢印の出し方（スキャン画面、面が見つかっていない側）は子どもテストで決めるため、文はまだない。
