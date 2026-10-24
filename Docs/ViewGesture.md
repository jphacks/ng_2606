# 視点の操作（ViewGesture）の出す量

担当：D（D_ui.md Step 4）。受ける人：C（C_gameplay.md Step 6 の ViewCamera）。

00_overview.md 4.6 には単位の決まりがないため、D が仮に次のように決めた。C の都合で変えたいときは D に伝える（多くは設定アセットの値を変えるだけで済む）。

| 依頼 | 一緒に渡す値 | 単位と向き |
| --- | --- | --- |
| OrbitView | Vector2（横、縦） | 度。指を右になぞると横が正、上になぞると縦が正 |
| ZoomView | float | m。2本指を広げると正（近づく）、つまむと負（離れる） |

- 感度は Assets/_Project/UI/ViewGestureSettings.asset で変える（初期値：画面の高さぶんなぞると180度、2本指の間隔が画面の高さぶん広がると0.6m）
- 縦の向きを逆にしたいときは、設定の InvertVertical をオンにする
- スティックとボタンの上で始まった指は、視点に使わない
- PC で試すときは、マウスの左ボタンでなぞると OrbitView、ホイールで ZoomView（1目盛り0.03m）
