# Semantic Kernel

[![Python package](https://img.shields.io/pypi/v/semantic-kernel)](https://pypi.org/project/semantic-kernel/)
[![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.SemanticKernel)](https://www.nuget.org/packages/Microsoft.SemanticKernel/)
[![dotnet](https://github.com/microsoft/semantic-kernel/actions/workflows/dotnet-ci.yml/badge.svg?branch=main)](https://github.com/microsoft/semantic-kernel/actions/workflows/dotnet-ci.yml)
[![License: MIT](https://img.shields.io/github/license/microsoft/semantic-kernel)](https://github.com/microsoft/semantic-kernel/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/SKDiscord)

> ℹ️ **注**: このプロジェクトは初期のアルファ段階にあり、AIと同様に急速に進化します。
> セマンティックカーネルの開発にご参加ください！
GitHub [Discussions](https://github.com/microsoft/semantic-kernel/discussions),
> opening GitHub [Issues](https://github.com/microsoft/semantic-kernel/issues/new/choose)への書き込み,
> [PRs](https://github.com/microsoft/semantic-kernel/pulls)を送ったり,
> または [Discord community](https://aka.ms/SKDiscord)
> に参加して貢献することができます。

**Semantic Kernel (SK)** は、AI大規模言語モデル(LLM)と従来のプログラミング言語の統合を可能にする軽量なSDKです。SK拡張可能なプログラミングモデルは、自然言語 **セマンティック関数**、従来のコード **ネイティブ関数**、および**埋め込みベースのメモリ**を組み合わせて、AIを使用してアプリケーションに新たな可能性と付加価値をもたらします。

SKは、[プロンプト テンプレート](docs/PROMPT_TEMPLATE_LANGUAGE.md)、関数チェーン、[ベクトル化されたメモリ](docs/EMBEDDINGS.md)、および[インテリジェント計画](docs/PLANNER.md)機能をすぐにサポートします。

![image](https://user-images.githubusercontent.com/371009/221739773-cf43522f-c1e4-42f2-b73d-5ba84e21febb.png)

Semantic Kernel は、最新のAI研究からのいくつかの設計パターンをサポートおよびカプセル化するように設計されているため、開発者は、[プロンプト](docs/PROMPT_TEMPLATE_LANGUAGE.md)チェーン、再帰的推論、要約、ゼロ/少数ショット学習、コンテキストメモリ、長期メモリ、[埋め込み](docs/EMBEDDINGS.md)、セマンティックインデックス作成、[計画](docs/PLANNER.md)、外部ナレッジストアや独自のデータへのアクセスなどの複雑な[スキル](docs/SKILLS.md)をアプリケーションに注入できます。

SKコミュニティに参加することで、AIファーストのアプリをより迅速に構築し、SDKがどのように構築されているかを最前列で覗くことができます。SKはオープンソースとしてリリースされており、より多くの先駆的な開発者がコンピューティングの歴史におけるこの画期的な瞬間の未来を築くために私たちに参加することができます。



## Semantic Kernel の使用を開始する⚡

Semantic Kernel は、C# と Python を使用してAIの探索と、アプリを構築するために使用できます:

<div style="display:flex;height:30px;padding:5px 0 5px 10px;">
<img src="https://user-images.githubusercontent.com/371009/230673036-fad1e8e6-5d48-49b1-a9c1-6f9834e0d165.png" style="margin-right:12px" height="30"/>
<a href="dotnet/README.md">C# で Semantic Kernel を使う</a>
</div>

<div style="display:flex;height:30px;padding:5px 0 5px 10px;">
<img src="https://user-images.githubusercontent.com/371009/230673733-7a447d30-b48e-46e1-bd84-2b321c90649e.png" style="margin-right:12px" height="30"/>
<a href="python/README.md">Python で Semantic Kernel を使う</a>
</div>
<br/>

C# と Python で使用できる機能の詳細については、[Feature Matrix](FEATURE_MATRIX.md)を参照してください。

基本的な作業を開始する最も簡単な方法は、API キー (OpenAI または Azure OpenAI) を取得し、C# または Python コンソール アプリケーション/スクリプトのいずれかを実行することです:

C# の場合:
1. 新しいコンソール アプリを作成します。
2. Semantic kernel nuget `Microsoft.SemanticKernel` を追加します。
3. [ここ](dotnet/README.md)からコードをコピーし、アプリの `Program.cs` ファイルにペーストします。
4. API キーとその他のパラメーターの構成プレースホルダーを、実際のキーと設定に置き換えます。
5. `F5` キーまたは `dotnet run` で実行する

Python の場合:
1. pipパッケージをインストールします: `python -m pip install semantic-kernel`
2. `hello-world.py` のようなスクリプトを作成します。
3. [ここ](python/README.md)で説明するように、API キーと設定を .`.env` ファイルに保存します。
4. [ここ](python/README.md)からコードを `hello-world.py` スクリプトにコピーします。
5. Python スクリプトを実行する。


## サンプル アプリ ⚡

リポジトリには、React フロントエンドと Semantic Kernel を使用するバックエンド Web サービスを含むいくつかのサンプルアプリケーションが含まれています。

これらのアプリの実行に関する詳細と手順については、リンク先をご覧ください。

|                                                                         |                                                                                                                                   |
| ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| [Simple chat summary](samples/apps/chat-summary-webapp-react/README.md) | すぐに使用できるスキルを使用して、それらのスキルをアプリに簡単に取り入れることができます。                                                                |
| [Book creator](samples/apps/book-creator-webapp-react/README.md)        | プランナーを使って複雑な目標を分解し、アプリでプランナーを使用することを想定します。                                             |
| [Authentication and APIs](samples/apps/auth-api-webapp-react/README.md) | 基本的なコネクタ パターンを使用して API を認証して接続し、外部データをアプリの LLM AI に統合することを想定します。 |
| [GitHub repository Q&A](samples/apps/github-qna-webapp-react/README.md) | 埋め込みとメモリを使用して最近のデータを格納し、それに対してクエリを実行できるようにします。                                                 |
| [Copilot Chat Sample App](samples/apps/copilot-chat-app/README.md)      | セマンティックカーネルに基づいて独自のチャットエクスペリエンスを構築します。                                                                          |

**必要条件:**

- 開始するには、[Open AI API Key](https://openai.com/api/)または [Azure Open AI service key](https://learn.microsoft.com/azure/cognitive-services/openai/quickstart?pivots=rest-api)が必要です。
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)は、サンプル Web アプリで使用されるローカル Web サービスとしてカーネルを実行するために必要です。
- [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)。.NET 7 がインストールされている場合は、Azure Function
  ツールには引き続き .NET 6 が必要なため、両方をインストールすることをお勧めします。
- [Yarn](https://yarnpkg.com/getting-started/install)は、Webアプリの依存関係をインストールするために使用されます。


## Jupyter Notebooks ⚡

より実践的な概要については、以下の C# および Python Jupyter Nptebooks を確認することもできます。:
* [Getting Started with C# notebook](samples/notebooks/dotnet/00-getting-started.ipynb)
* [Getting Started with Python notebook](samples/notebooks/python/00-getting-started.ipynb)

**必要条件:** C# ノートブックには、[.NET 7](https://dotnet.microsoft.com/download) と VS Code [Polyglot extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode)機能が必要です。

## 貢献とコミュニティ

SKコミュニティへの貢献や提案を歓迎します!最も簡単なものの1つ
参加する方法は、GitHubリポジトリでディスカッションに参加することです。
バグレポートと修正は大歓迎です!

新機能、コンポーネント、または拡張機能については、PRを送信する前に問題を開いて話し合ってください。これは、コアを別の方向に進める可能性があるための拒否を回避するためだけでなく、より大きなエコシステムへの影響を考慮するためです。

詳細を確認して開始するには:

- [ドキュメント](https://aka.ms/sk/learn)を読む
- プロジェクトに[貢献](https://github.com/microsoft/semantic-kernel/blob/main/CONTRIBUTING.md)する方法を学ぶ
- [Discode コミュニティ](https://aka.ms/SKDiscord)に参加する
- [ブログ](https://aka.ms/sk/blog)でチームをフォローする

## Code of Conduct

This project has adopted the
[Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the
[Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com)
with any additional questions or comments.

## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT](LICENSE) license.
