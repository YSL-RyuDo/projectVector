# Project Vector

[English](README.md) · [Korean](README.ko.md) · [Japanese](README.ja.md)

チームで開発中のUnityマルチプレイヤーFPSです。ゲームサーバー連携と、試合・プレイヤー・継続的な進行情報を結ぶシステムを担当しています。

## 担当した内容

- センチネルを含むキャラクタースキルのパケット処理とゲームプレイ連携を実装しました。
- マッチングと試合・結果演出、キャリア・プロフィール情報、フレンド、ショップ連携、再接続・音声のクライアント側フローを担当しました。
- チーム提供のモデル・モーション統合、ADS調整、訓練場拡張、エディター検証ツールを担当しました。統合とモデル・アニメーション自体の制作は区別します。

## 貢献の根拠

- [Sentinel / 스킬 / スキル](https://github.com/YSL-RyuDo/projectVector/commit/756c9a09f32ed4a073d5d3d60008b1aaa44f0f60)
- [Practice & ADS / 훈련장·조준 / 訓練場・照準](https://github.com/YSL-RyuDo/projectVector/commit/f4457e2d77c251b9e0da5d976927303d04a64b43)
- [Reconnect & voice / 재접속·음성 / 再接続・音声](https://github.com/YSL-RyuDo/projectVector/commit/b62cbeeb16e72738d2dd7c491c8d42ec31afcd18)
- [Additional YSL work / YSL 추가 작업 / YSLの追加作業](https://github.com/YSL-RyuDo/projectVector/commit/5e5c3c53b010fb2807f6b53c0eb0f5ceec8d1350)

コミットは記載した変更の根拠であり、貢献率の点数ではありません。外部素材・チームメンバーの貢献は区別します。

## 実行・コードの確認

[ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)記載のUnity版で開いてください。パッケージロックとシリアライズ済み素材を維持し、Unityを開く前に対象ブランチを選んでください。

- Unity scene: [Assets/Scenes/Login.unity](Assets/Scenes/Login.unity)

[ProjectVectorServer](https://github.com/YSL-RyuDo/ProjectVectorServer)

## 開発・検証範囲

開発中です。文書化時点の`YSL`には`main`未反映の移動・照準・命中フィードバック・遠隔動作の変更があり、最新mainの変更をすべて含むわけでもありません。デモのコード版を明示してください。既存のエディター・ビルド検証と、2台のPCによる実音声通信・2クライアントの試合検証は区別します。同時接続規模や商用運用品質は主張しません。

## 詳細資料

- [Docs/NextWorkSession.md](Docs/NextWorkSession.md)
- [Docs/ShopGachaImplementation.md](Docs/ShopGachaImplementation.md)
