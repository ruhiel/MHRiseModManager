# MHRiseModManager

## 目次

- [MHRiseModManager](#mhrisemodmanager)
  - [目次](#目次)
  - [はじめに](#はじめに)
  - [使い方](#使い方)
    - [本体パスの設定](#本体パスの設定)
    - [インポート](#インポート)
    - [一括インポート](#一括インポート)
    - [インストール](#インストール)
    - [アンインストール](#アンインストール)
    - [登録削除](#登録削除)
    - [CSVエクスポート](#csvエクスポート)
    - [バージョンチェック](#バージョンチェック)
    - [Modの更新](#modの更新)
    - [Mod zipファイルの修正方法](#mod-zipファイルの修正方法)
    - [設定](#設定)

## はじめに

MHRiseModManagerはSteam版モンスターハンターライズおよびモンスターハンターライズ：サンブレイク向けに公開されているModの管理ソフトウェアです（モンスターハンターライズ本体と干渉しない外部ソフトウェアです）。

MHRiseModManagerがサポートするModは[nexusmods](https://www.nexusmods.com/monsterhunterrise)で公開されているModのうち[Reframework](https://www.nexusmods.com/monsterhunterrise/mods/26)本体、およびReframework向けのMODのみとなっています。

## 使い方

### 本体パスの設定

まずはじめにModをインストールする場所となる本体パスの設定を行います。
解凍したフォルダ内にあるMHRiseModManager.exeをダブルクリックし、アプリケーションを起動します。
まずアプリケーション右上の参照ボタンをクリックします。
![本体パスの設定1](https://user-images.githubusercontent.com/6840095/188249800-9f9ca804-ded5-47c2-8678-e336c6cc3089.png)

次にモンスターハンターライズ本体がインストールされているフォルダに移動し、開くボタンをクリックします。
![本体パスの設定2](https://user-images.githubusercontent.com/6840095/188249806-340c6c58-a98e-4e9a-abcf-66c26cea350e.png)


ゲームフォルダ欄にモンスターハンターライズ本体のフォルダが設定されて完了です。
![本体パスの設定3](https://user-images.githubusercontent.com/6840095/188249812-f7656be1-f760-44c4-8ee6-a44c8abc8b2c.png)

### インポート

Modを管理対象にするためにインポートが必要になります。
まずはじめに[nexusmods](https://www.nexusmods.com/monsterhunterrise)等からダウンロードしたModファイルを圧縮されている状態のまま、アプリケーション左のMod一覧に**ドラッグアンドドロップ**します。

![インポート1](https://user-images.githubusercontent.com/6840095/188249822-f1e44560-8100-48f5-8dac-0351f3727804.png)


ダイアログが表示されます。

![インポート2](https://user-images.githubusercontent.com/6840095/188249838-47b7e13d-168e-4281-9fcd-fb7b3731848c.png)
Mod名、URL、バージョン、メモの各任意項目を入力し、OKボタンをクリックします。
全て必須ではありませんが、URLを入力した場合Modのバージョンチェックに利用されます。
nexusmodsの各タブでバージョン情報が異なる場合があり、正しくバージョンチェックできない事があるため、URLはファイルをダウンロードしたタブのページに設定することをおすすめします（?tab=filesで終わるURLです）。

![インポート3](https://user-images.githubusercontent.com/6840095/188249842-1fee1bfc-2cc5-4a92-8108-9f97fe5ee9db.png)

インポート処理が実行されます。OKボタンクリックします。

![インポート4](https://user-images.githubusercontent.com/6840095/188249850-b17f6e84-702a-468d-964f-94fdd09d1c25.png)

この操作を管理したいModの数分だけ実施します。

![インポート5](https://user-images.githubusercontent.com/6840095/188249851-f1af4548-4ee1-40d5-b06c-dded793389c0.png)

### 一括インポート

複数のModをまとめてインポートしたい場合、一括インポート機能を利用することができます。

メニュー ⇒ Mod ⇒ CSVテンプレート出力をクリックします。
![一括インポート1](https://user-images.githubusercontent.com/6840095/188249867-9ced9039-06a6-4230-bc17-6db2b941e98d.png)

Excel、または、メモ帳などのテキストエディタが開かれます。
![一括インポート2](https://user-images.githubusercontent.com/6840095/188249870-817427a3-99de-4430-a6e7-0cce1693eee8.png)

CSVファイルを編集します。
左の列から順にMod名、URL、メモ、バージョン、ファイルのフルパスを入力します。
![一括インポート3](https://user-images.githubusercontent.com/6840095/188249884-09130fcd-db2d-4157-b9ee-e44e28b6b8c2.png)

メニュー ⇒ Mod ⇒ CSVインポートをクリックします。
![一括インポート4](https://user-images.githubusercontent.com/6840095/188249887-545d1e98-1acf-4b89-85ce-95b0214fd22a.png)

さきほど編集したCSVファイルを選択し、開くボタンをクリックします。
![一括インポート5](https://user-images.githubusercontent.com/6840095/188249891-d37b306f-a94c-4ab4-826e-a91b6c0c639a.png)

一括インポートされ、Modが管理対象になります。
![一括インポート6](https://user-images.githubusercontent.com/6840095/188249898-4eea3a7e-7020-4abb-8c3c-688971e0b9db.png)
![一括インポート7](https://user-images.githubusercontent.com/6840095/188249902-9e5bf60a-dc66-426a-85a5-834a55ba5240.png)

### インストール

インポートしたModをインストールするには対象のModをMod一覧より選択し、右下のインストールボタンをクリックします。
![インストール1](https://user-images.githubusercontent.com/6840095/188249911-7166f8ee-b0ad-4415-b899-4d8832a6b1e3.png)

Mod一覧の状態がインストール済みになりインストールされます。
![インストール2](https://user-images.githubusercontent.com/6840095/188249916-924cd50f-bd5b-45b2-879f-bca5db9e1aaa.png)

インストールされていないModをまとめてインストールする事もできます。
メニュー ⇒ Mod ⇒ 一括インストールをクリックします。
![インストール3](https://user-images.githubusercontent.com/6840095/188249924-ecfc345f-285d-42a0-ac8d-4217d2ae9df7.png)

OKボタンをクリックします。
![インストール4](https://user-images.githubusercontent.com/6840095/188249928-9e2b7819-7909-4d8c-a1c9-5ff4813fafef.png)

Mod一覧の状態がインストール済みになりインストールされます。
![インストール5](https://user-images.githubusercontent.com/6840095/188249934-95ce44c6-46f4-4c58-bce9-296173921e16.png)

### アンインストール

インストールしたModをアンインストールするには対象のModをMod一覧より選択し、右下のアンインストールボタンをクリックします。
![アンインストール1](https://user-images.githubusercontent.com/6840095/188249944-1942e2c9-74c6-41bb-96bd-fa72c8589c55.png)

Mod一覧の状態が未インストールになりアンインストールされます。
![アンインストール2](https://user-images.githubusercontent.com/6840095/188249946-adc8e120-221b-42ec-881e-40fd95b25334.png)

インストール済みのModをまとめてアンインストールする事もできます。
メニュー ⇒ Mod ⇒ 一括アンインストールをクリックします。
![アンインストール3](https://user-images.githubusercontent.com/6840095/188249949-ee8edef3-fefc-4bb9-a8ce-c98f1f7c3431.png)

OKボタンをクリックします。
![アンインストール4](https://user-images.githubusercontent.com/6840095/188249953-539b7f39-4437-487c-84c6-0d14588e903f.png)

Mod一覧の状態が未インストールになりアンインストールされます。
![アンインストール5](https://user-images.githubusercontent.com/6840095/188249955-3c3602da-40cb-4bbc-b256-c4654040ac6a.png)


### 登録削除

アンインストール済みのModを一覧から削除するには登録削除を行います。対象のModをMod一覧より選択し、右下の登録削除ボタンをクリックします。
![登録削除1](https://user-images.githubusercontent.com/6840095/188249984-c1639da5-f63e-4945-96b3-350dbbcf5e9b.png)

はいをクリックします。
![登録削除2](https://user-images.githubusercontent.com/6840095/188249990-44fe409c-4ec8-4d79-8c3c-326aa3ef9f53.png)

OKボタンをクリックします。
![登録削除3](https://user-images.githubusercontent.com/6840095/188249993-2cbd8bf7-9dda-465e-8a21-40462d02d406.png)

Mod一覧からModが削除されます。
![登録削除4](https://user-images.githubusercontent.com/6840095/188249997-76b04d36-aee4-438b-99d4-fe2f580ba76a.png)

未インストールのModをまとめて登録削除する事もできます。
メニュー ⇒ Mod ⇒ 一括登録削除をクリックします。
![登録削除5](https://user-images.githubusercontent.com/6840095/188250000-d8d4bdda-a782-4e41-a504-67711ad3595e.png)

はいをクリックします。
![登録削除6](https://user-images.githubusercontent.com/6840095/188250002-37469a3f-1c51-4b0b-a82c-1af3cd55c484.png)

OKボタンをクリックします。
![登録削除7](https://user-images.githubusercontent.com/6840095/188250005-495445a8-4868-4b4a-93ee-92561c35f8b5.png)

Mod一覧からModが一括削除されます。
![登録削除8](https://user-images.githubusercontent.com/6840095/188250008-199785f7-aeb2-454e-ab24-f4f1c4954d55.png)

### CSVエクスポート

Mod一覧をCSVファイルにエクスポートできます。このファイルにフルパスを追記するだけでインポート用CSVを作成できます。エクスポートするには、メニュー ⇒ Mod ⇒ CSVエクスポートをクリックします。
![CSVエクスポート1](https://user-images.githubusercontent.com/6840095/188250024-3151f1e4-fa2b-43fe-af2d-4e529d793f86.png)

CSVファイルが開きます。
![CSVエクスポート2](https://user-images.githubusercontent.com/6840095/188250038-b82f94ce-2886-4007-aac8-5a1aaaeccc49.png)


### バージョンチェック

Modの最新バージョンをチェックするには、メニュー ⇒ Mod ⇒ バージョンチェックをクリックします。

![バージョンチェック1](https://user-images.githubusercontent.com/6840095/188250045-a7d728c7-5417-4852-9853-0da1ca79fcdc.png)

![バージョンチェック2](https://user-images.githubusercontent.com/6840095/188250050-2f8b5c82-28c2-49db-9f1d-e6f37413abfe.png)

バージョンチェック結果が表示されるので、OKボタンをクリックします。

![バージョンチェック3](https://user-images.githubusercontent.com/6840095/188250053-2bddfae2-fc91-4fbc-a957-4d24c59080f1.png)

各Modを選択すると最新バージョン番号が表示されます。

![バージョンチェック4](https://user-images.githubusercontent.com/6840095/188250056-e5d24861-fafa-4cab-a9da-7fbe6b401cc8.png)

### Modの更新

Modを最新にバージョンバージョンアップしたり、zipファイルのファイル構成を修正するにはModの更新を行います。各ModのMod更新ボタンをクリックします。

![Mod更新1](https://user-images.githubusercontent.com/6840095/188250061-cf3c4cbc-ffcd-45cf-b6b9-9ffad6c22f6a.png)

Zipファイルを選択して開くボタンをクリックします。
![Mod更新2](https://user-images.githubusercontent.com/6840095/188250065-ca9cae0f-bdc3-4b9e-8ef6-2a54f0a9568c.png)

画面に戻り、更新処理が終了します。
![Mod更新3](https://user-images.githubusercontent.com/6840095/188250067-2a9f5b2c-82f9-4200-b76d-5bd191c76afa.png)

### Mod zipファイルの修正方法

多くの[Reframework](https://www.nexusmods.com/monsterhunterrise/mods/26)用Modは適切なフォルダ階層にファイルを配置しないと動作しないようになっています。
例えばこのModはreframeworkフォルダ直下となっており階層が正しくないため動作しません。
本項では、zipファイルの修正方法について説明します(OSのファイルシステム操作の説明であり、ちょっとずれた項目ですがModファイル全てが正しく公開されていないため補足します)。
![zipファイルの修正1](https://user-images.githubusercontent.com/6840095/188250076-5695a5b2-8363-416a-bfb0-b1ab6437dad3.png)

zipファイルを解凍し、スクリプト本体のフォルダに移動します。
![zipファイルの修正2](https://user-images.githubusercontent.com/6840095/188250079-0acf968e-c83c-49ad-b9b4-677ca2db3fa5.png)

autorunフォルダ、reframeworkフォルダを作成します。
![zipファイルの修正3](https://user-images.githubusercontent.com/6840095/188250084-e94eeb7f-3c9d-4513-8bac-3bb90ad4992b.png)

スクリプト本体をautorunフォルダに移動します。
![zipファイルの修正4](https://user-images.githubusercontent.com/6840095/188250089-dfdf7f5a-e6b9-455e-b41f-1ed951a381f7.png)

autorunフォルダをreframeworkフォルダに移動します。
![zipファイルの修正5](https://user-images.githubusercontent.com/6840095/188250092-c70666bd-d048-4f7b-ad97-2de57b8d53f8.png)

reframeworkフォルダをzip圧縮します。
![zipファイルの修正6](https://user-images.githubusercontent.com/6840095/188250096-91b1c451-f1dd-4876-a192-aa715e9e6868.png)

ファイル名を元の名前に変更して修正完了です。
![zipファイルの修正7](https://user-images.githubusercontent.com/6840095/188250099-6eadf57b-214c-4343-8687-bc08d5aef69b.png)

[Modの更新](#modの更新)の手順でさきほどのzipファイルにより更新します。
正しい階層で修正された事が確認できます。
![zipファイルの修正8](https://user-images.githubusercontent.com/6840095/188250102-b0013060-3a0e-42b7-928e-b0f78763e6ce.png)

### 設定

設定画面を開くにはメニュー ⇒ 設定をクリックします。
![設定1](https://user-images.githubusercontent.com/6840095/188250109-9ef6acc7-dd95-4934-98aa-831724f36e96.png)

初期起動時のバージョンチェック、および、設定初期化、テーマカラーの変更を行うことができます。
![設定2](https://user-images.githubusercontent.com/6840095/188250113-f0c7d6f0-f151-4350-a578-d9b9e009adcb.png)

