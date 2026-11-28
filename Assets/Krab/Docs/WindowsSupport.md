# Windows対応

Krabは macOS / Windows のEditorとStandalone Playerで動かすことを想定している。OSで違うのは映像出力とMIDIポートだけで、それ以外のコードは共通。

## 映像出力（Syphon / Spout）

`Assets/Krab/Common/Prefabs/OutputSender.prefab` に、両方の送出コンポーネントを載せている。

| OS | Controller | 送出コンポーネント | 送信名のPrefsキー |
| --- | --- | --- | --- |
| macOS | `SyphonController` | `SyphonServer`（Klak Syphon） | `SpoutName`（過去の命名のまま） |
| Windows | `SpoutController` | `SpoutSender`（Klak Spout） | `SpoutSenderName` |

- `SyphonServer` / `SpoutSender` はPrefab上では無効で保存する。各Controllerが `Awake` で `Application.platform` を見て、対応するOSの方だけを有効にする。違うOSのネイティブプラグインを読み込まないようにするためで、Editorで再生していないときも同じ。
- 送出コンポーネントをInspectorで有効にしたまま保存しない。違うOSで開くと、編集中でもネイティブ呼び出しのエラーが出る。
- 設定UIの「Common/Output」には両方が並ぶ。対応していないOSの方には「〜 only」とだけ表示する。
- どちらも `IOutputTextureContainer` の `TextureChanged` を購読し、出力RTをTextureモードで送る。

## MIDI

- 入力（Minis）と出力（RtMidi）はWindowsにも対応している。Windowsでは WinMM を使う。
- `MidiOutput._portNameFilter` は大文字小文字を区別しない部分一致で探す。Windowsで付く `2- APC mini mk2` のような接頭辞でも一致する。
- WindowsではAPC mini mk2の2つ目のポートが `MIDIOUT2 (APC mini mk2)` と表示される。LEDを送るのは1つ目のポートなので、`MIDIOUT` で始まる名前は後回しにする。
- WinMMのポートは、1つのアプリしか開けない。他のDAWなどがAPC mini mk2を開いていると、Krab側は接続できない。`MidiOutput` は `_reconnectInterval` ごとに接続を試し直す。

## 設定の保存先

- PrefsGUIの設定（MIDI Binding、Syphon/Spoutの送信名なども含む）は、OSによって保存先が違う。
  - macOS：`Application.persistentDataPath`
  - Windows：`UiBuilder.prefab` の `PrefsKvsPathCustom` で決まる `%dataPath%/../../%productName%Prefs`。Editorではプロジェクトフォルダと同じ階層、Playerではビルドフォルダと同じ階層になる。
- ShapeSynthのプリセット・共通Value・評価は、どちらのOSでも `StreamingAssets/Krab/ShapeSynth/`（Git管理）。
- MacとWindowsの間で設定は自動では引き継がれない。必要ならJSONを手でコピーする。

## Windows実機での確認項目（未実施）

2026-11-18時点では、Windows実機での確認を一度もしていない。Macでは、コンパイルが通ることと、Play ModeでSyphonServerだけが有効になりSpoutSenderは無効のままであることを確認した。

- [ ] プロジェクトを開き、Packageの解決とコンパイルでエラーが出ない
- [ ] EditModeテストがすべて成功する
- [ ] 各Sample Sceneと本番ScenesをPlayしたとき、Syphon関連のエラーが出ない
- [ ] Spoutの受信側（Resolume、OBS、Spout Receiverなど）で、`krab` を選ぶと出力RTが見える
- [ ] 出力解像度を変えたあともSpoutが追従する
- [ ] APC mini mk2の入力（Pad / Fader）とLED出力が動く。`MidiOutput.PortName` が意図したポートになっている
- [ ] Windows Standalone Playerでビルドし、Spout出力・MIDI・設定の保存先を確認する
