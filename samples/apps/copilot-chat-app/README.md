# Copilot チャット サンプル アプリケーション
> このサンプルは教育のみを目的としており、運用環境のデプロイにはお勧めしません。

# Copilot チャットについて
このサンプルでは、独自の統合された大規模言語モデルのチャット copilot を作成できます。
これは、コマンド メッセージ、ユーザーの意図、メモリなどの複数の動的コンポーネントを備えた、強化されたインテリジェンス アプリです。

チャットのプロンプトと応答は、ユーザーとアプリケーション間の会話が進むにつれて進化します。
このチャットエクスペリエンスは、セマンティックカーネルと、各応答を構築するために連携する多数の関数を含む Copilot チャットスキルで調整されます。

![UI Sample](images/UI-Sample.png)

# 環境を構成する
開始する前に、次の要件が満たされていることを確認してください:
- [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [Node.js](https://nodejs.org/en/download)
- [Yarn](https://classic.yarnpkg.com/lang/en/docs/install)
- [Visual Studio Code](https://code.visualstudio.com/Download) **(オプション)** 
- [Azure OpenAI](https://aka.ms/oai/access) リソースまたは [OpenAI](https://platform.openai.com)のアカウント。

# WebApi バックエンド サーバーの起動
このサンプルでは、フロントエンド Web UI とバックエンド API サーバーの 2 つのアプリケーションを使用します。
まず、バックエンド API サーバーを設定して確認しましょう。

1. ローカルホスト開発者証明書を生成して信頼します。ターミナルを開き、次のコマンドを実行します:
   - Windows および Mac の場合は、`dotnet dev-certs https --trust` を実行し、この証明書をインストールするかどうかを尋ねられたら `Yes` を選択します。
   - Linux の場合は、`dotnet dev-certs https` を実行します。
   > **注:** 開発者証明書をインストールした後、Web ブラウザーのすべてのインスタンスを閉じることをお勧めします。

2. `samples/apps/copilot-chat-app/webapi` に移動し、`appsettings.json` を開きます。
   - `Completion` および `Embedding` 構成セクションを更新します:
     - `AIService` を、使用する AI サービス (`AzureOpenAI` または `OpenAI` など) に更新します。
     - Azure OpenAI を使用している場合は、エンドポイントを Azure OpenAI リソースのエンドポイント アドレスに更新します (例: `http://contoso.openai.azure.com`)。
        > OpenAI を使用している場合、このプロパティは無視されます。
     - webapi プロジェクト ディレクトリでターミナルを開き、`dotnet user-secrets` を使用して、Azure OpenAI キーを設定します。
       ```bash
       cd semantic-kernel/samples/apps/copilot-chat-app/webapi
       dotnet user-secrets set "Completion:Key" "MY_AZUREOPENAI_OR_OPENAI_KEY"
       dotnet user-secrets set "Embedding:Key" "MY_AZUREOPENAI_OR_OPENAI_KEY"
       ```
     - `DeploymentOrModelID` を、使用する Azure OpenAI デプロイまたは OpenAI モデルに更新します。
       - `Completion` のために、CopilotChat は gpt-3.5-turbo や gpt-4 などのチャット補完モデルに最適化されています。
         > **重要:** gpt-3.5-turbo は通常、Azure OpenAI では "gpt-35-turbo" (ピリオドなし)、OpenAI では "gpt-3.5-turbo" (ピリオドあり) とラベル付けされます。
       - `Embedding` には、埋め込みを生成するための `text-embedding-ada-002` で十分であり、費用対効果が高いです。
   
   - **(オプション)** チャット入力の音声テキスト変換を有効にするには、`AzureSpeech` 構成セクションを更新します。
     > まだ作成していない場合は、[Azure Speech リソースを作成](https://ms.portal.azure.com/#create/Microsoft.CognitiveServicesSpeechServices)する必要があります (詳細については、[./webapi/appsettings.json](webapi/appsettings.json) を参照してください)。
     - `Region` を、Speech SDK インスタンスに適したリージョンに更新します。
     - webapi プロジェクト ディレクトリのターミナルを開き、`AzureSpeech:Key` の dotnet のユーザー シークレット値を設定することで、Azure 音声キーを設定します。
       ```bash
       dotnet user-secrets set "AzureSpeech:Key" "MY_AZURE_SPEECH_KEY" 
       ```

3. バックエンド API サーバーを構築して実行する
    1. ターミナルを開き、`samples/apps/copilot-chat-app/webapi` に移動します
    
    2. `dotnet build` を実行してプロジェクトをビルドします。
    
    3. `dotnet run` を実行してサーバーを起動します。
    
    4. バックエンド サーバーが応答していることを確認し、Web ブラウザーを開いて `https://localhost:40443/probe`　に移動します。
       > probe に初めてアクセスすると、Webサイトの証明書に問題があるという警告が表示される場合があります。
         受け入れる/続行するオプションを選択します-これは `localhost` でサービスを実行するときに予想されます フロントエンドがバックエンドと通信できるようにする前に、ブラウザが証明書を受け入れる必要がある場合があるため、これを行うことが重要です。

      > また、Windows Defender ファイアウォールを確認し、必要に応じてアプリがプライベート ネットワークまたはパブリック ネットワーク経由で通信できるようにする必要がある場合もあります。
 
4. フロントエンドアプリケーションを構築して起動する
   1. また、Azure Active Directory (AAD) アプリケーションの登録も必要です。
      > アプリケーション登録の作成の詳細については、[こちら](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)を参照してください。
      - プラットフォームの種類として `Single-page application (SPA)` を選択し、Web リダイレクト URI を `https://localhost:3000` に設定します。
      - このサンプルでサポートされているアカウントの種類として、`任意の組織のディレクトリ内のアカウント と 個人用 Microsoft アカウント` を選択します。
      - Azure ポータルから `Application (client) ID` をメモしておいてください。後で使用します。


   2. ターミナルを開き、`samples/apps/copilot-chat-app/webapp` に移動します `env.example` を新しいファイル `.env` にコピーし、上記で作成した AAD アプリケーション (クライアント) ID で `REACT_APP_AAD_CLIENT_ID` を更新します。例：
      ```bash
      REACT_APP_BACKEND_URI=https://localhost:40443/
      REACT_APP_AAD_CLIENT_ID=00000000-0000-0000-0000-000000000000
      ```
   
   3. フロントエンド アプリケーションをビルドして実行するには
      ```bash
      yarn install
      yarn start
      ```
   
   4. バックエンドとフロントエンドを実行すると、Webブラウザが自動的に起動し、`http://localhost:3000` に移動します
      > フロントエンド アプリケーションを初めて実行すると、起動に数分ほどかかる場合があります。
   
   5. Microsoft の個人アカウントまたは "職場または学校" アカウントでサインインします。

   6. アプリケーションがあなたのプロフィール情報(例えばあなたの名前)を読み取るための同意許可。
    
    エラーや問題が発生した場合は、以下のトラブルシューティングのセクションを参照してください。

5. 楽しんでください！
   > **注:** 各チャット操作は、課金される可能性のあるトークンを使用する Azure OpenAI/OpenAI を呼び出します。

## トラブルシューティング
### 1. ローカルホストの SSL 証明書のエラー
![](images/Cert-Issue.png)

上記のようなエラーメッセージで停止する場合は、ブラウザがフロントエンドアクセスをブロックしている可能性があります
接続の許可を待っている間にバックエンドに移動します。これを解決するには、次のことを試してください:

1. Web ブラウザーを開き、`https://localhost:40443/probe` に移動して、バックエンド サービスが実行されていることを確認します
   - 確認メッセージが表示されます:`Semantic Kernel service is up and running`
2. ブラウザから安全でないWebサイトにアクセスするリスクを確認するように求められた場合は、フロントエンドがバックエンドサーバーへの接続を許可する前に、メッセージを確認する必要があります。
   - 確認応答、続行、および「Semantic Kernel service is up and running」というメッセージが表示されるまで移動します。
3. `https://localhost:3000` に移動するか、ページを更新して Copilot チャットアプリケーションを使用します。


### 2. リポジトリ/フォークを更新した後の構成の問題。
[PR #470](https://github.com/microsoft/semantic-kernel/pull/470) の時点で、トップレベルの構成キーの一部が更新されました。特に、
  - `CompletionConfig` は現在は `Completion` に変更されています
  - `EmbeddingConfig` は現在は `Embedding` に変更されています
  
また、`dotnet user-secrets set` が設定されたシークレット セットに使用されるキーを更新する必要がある場合もあります。

### 3. テキスト補完モデル (`text-davinci-003` など) を使用した場合の問題

[PR #499](https://github.com/microsoft/semantic-kernel/pull/499) の時点で、Copilot Chat は現在、`gpt-3.5-*` や `gpt-4-*` などのチャット完了モデルのサポートに重点を置いています。 チャット補完をサポートする現在のモデルの完全なリストについては、[OpenAI's model compatiblity](https://platform.openai.com/docs/models/model-endpoint-compatibility)を参照してください。

## 関連資料

1. [Import Document Application](./importdocument/README.md): Import a document to the memory store.
