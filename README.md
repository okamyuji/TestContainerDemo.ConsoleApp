# TestContainerDemo

TestContainerDemoプロジェクトは、.NET Framework 4.8.1を使用してDockerコンテナ上でOracleおよびPostgreSQLデータベースのCRUD操作を実行し、テストを行うためのデモアプリケーションです。

## 環境要件

このプロジェクトを実行するには、以下の環境が必要です

- .NET Framework 4.8.1
- Visual Studio 2019または2022
- Docker Desktop
- Docker上のデータベースイメージ:
  - PostgreSQL (`postgres:latest`)
  - Oracle (`gvenzl/oracle-xe:latest`)

## プロジェクト構成

プロジェクト構成は以下の通りです

```
TestContainerDemo/
├── TestContainerDemo.sln
├── TestContainerDemo.ConsoleApp/
│   ├── Models/
│   │   └── Customer.cs
│   ├── Repositories/
│   │   ├── ICustomerRepository.cs
│   │   ├── OracleCustomerRepository.cs
│   │   └── PostgresCustomerRepository.cs
│   └── Program.cs
└── TestContainerDemo.Tests/
    ├── Helpers/
    │   └── DockerContainerHelper.cs
    ├── OracleCustomerRepositoryTests.cs
    └── PostgresCustomerRepositoryTests.cs
```

## セットアップ手順

### 1. プロジェクトのセットアップ

1. リポジトリをクローンまたはダウンロードします
2. Visual Studioでソリューション（`TestContainerDemo.sln`）を開きます
3. NuGetパッケージの復元を行います

### 2. 必要なNuGetパッケージ

コンソールアプリケーションプロジェクトには以下のパッケージをインストールします

```
Install-Package Oracle.ManagedDataAccess -Version 23.8.0
Install-Package Npgsql -Version 8.0.7 # 最新の9.0.3は.NET Framework 4.8.1では使用できません
```

テストプロジェクトには以下のパッケージをインストールします：

```
Install-Package MSTest.TestAdapter -Version 3.8.3
Install-Package MSTest.TestFramework -Version 3.8.3
Install-Package Docker.DotNet -Version 3.125.15
```

### 3. Dockerイメージのプル

必要なDockerイメージをプルします

```bash
docker pull postgres:latest
docker pull gvenzl/oracle-xe:latest
```

## Docker Test Containersの実装

本プロジェクトでは、テスト時にDockerコンテナを動的に起動・停止する機能を実装しています。以下は主要な機能です

### DockerContainerHelperクラス

`DockerContainerHelper`クラスはDockerコンテナの管理を抽象化し、以下の機能を提供します

- コンテナの作成と起動
- 環境変数の設定
- ポートマッピングの設定
- コンテナの停止と削除

```csharp
// 使用例
var container = new DockerContainerHelper("postgres:latest")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithPortMapping("15432", "5432/tcp");

await container.StartAsync();
// ... コンテナを使用した処理 ...
await container.StopAsync();
```

## リポジトリパターンの実装

本プロジェクトではリポジトリパターンを採用しています

### ICustomerRepositoryインターフェース

データアクセスを抽象化するインターフェースで、以下のCRUD操作を定義しています：

- `CreateCustomerAsync` - 顧客情報の登録
- `GetCustomerByIdAsync` - ID指定による顧客情報の取得
- `GetAllCustomersAsync` - 全顧客情報の取得
- `UpdateCustomerAsync` - 顧客情報の更新
- `DeleteCustomerAsync` - 顧客情報の削除

このインターフェースにより、データベース実装の詳細を隠蔽し、異なるデータベースに対して同一のインターフェースでアクセスできます。

## Oracle実装の特徴

`OracleCustomerRepository`クラスは、Oracle Database固有の実装を提供します

### 主な特徴

- **Oracle.ManagedDataAccess**ライブラリの使用
- シーケンスとトリガーによるID自動生成
- PL/SQLを使用したテーブル初期化処理
- 名前付きパラメータ（`:parameter`形式）の使用
- OUTパラメータを使用した生成IDの取得

### Oracle固有のSQL例

```sql
-- シーケンス作成
CREATE SEQUENCE CUSTOMERS_SEQ START WITH 1 INCREMENT BY 1

-- トリガーによるID自動生成
CREATE OR REPLACE TRIGGER CUSTOMERS_BI_TRG 
BEFORE INSERT ON CUSTOMERS 
FOR EACH ROW 
BEGIN 
    IF :NEW.ID IS NULL THEN 
        SELECT CUSTOMERS_SEQ.NEXTVAL INTO :NEW.ID FROM DUAL; 
    END IF; 
END;
```

## PostgreSQL実装の特徴

`PostgresCustomerRepository`クラスは、PostgreSQL固有の実装を提供します

### 主な特徴

- **Npgsql**ライブラリの使用
- SERIAL型によるID自動生成
- パラメータ指定（`@parameter`形式）の使用
- RETURNING句による生成IDの直接取得

### PostgreSQL固有のSQL例

```sql
-- SERIAL型によるID自動連番
CREATE TABLE IF NOT EXISTS customers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    created_at TIMESTAMP NOT NULL
)

-- RETURNING句による生成IDの取得
INSERT INTO customers (name, email, created_at) 
VALUES (@Name, @Email, @CreatedAt) 
RETURNING id
```

## テスト実装

本プロジェクトは、MSTestフレームワークを使用して各リポジトリの実装をテストしています。

### テストの特徴

- Docker上のデータベースコンテナを動的に起動・停止
- 一意のコンテナ名による分離
- 専用のポートマッピングによるポート競合の回避
- テスト後のリソース解放（コンテナの停止と削除）

### テスト実行の流れ

1. テストクラスの初期化時（`ClassInitialize`）にDockerコンテナを起動
2. データベースの準備（テーブル作成など）
3. 各テストメソッドの実行
4. テストクラスのクリーンアップ（`ClassCleanup`）でコンテナを停止・削除

## 使用例

### コンソールアプリケーションの使用

```csharp
// 環境変数で接続文字列を設定
Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", 
    "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres");

// PostgreSQLリポジトリを使用した例
var repository = new PostgresCustomerRepository(
    Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"));

// テーブルの準備
await repository.EnsureTableCreatedAsync();

// 顧客情報の登録
var customer = new Customer
{
    Name = "山田太郎",
    Email = "taro.yamada@example.com",
    CreatedAt = DateTime.Now
};

var newId = await repository.CreateCustomerAsync(customer);
Console.WriteLine($"登録されたID: {newId}");

// 顧客情報の取得
var retrievedCustomer = await repository.GetCustomerByIdAsync(newId);
Console.WriteLine($"取得した顧客情報: {retrievedCustomer}");
```

### テストの実行

1. Visual Studioのテストエクスプローラーからテストを実行
2. または、コマンドラインから`VSTest.Console.exe`を使用して実行

```
VSTest.Console.exe TestContainerDemo.Tests.dll
```

## 注意点

### Dockerの利用

- Dockerが起動していることを確認してください
- 必要なDockerイメージがプルされていることを確認してください
- ポート競合が発生する場合は、使用するポート番号を変更してください

### Oracle関連の注意点

- Oracleコンテナは起動に時間がかかる場合があります
- OracleのXEエディションはメモリ使用量が多いため、システムリソースに注意してください
- 接続文字列の形式に注意してください（特にサービス名の部分）

### PostgreSQL関連の注意点

- PostgreSQLコンテナはOracleと比較して軽量ですが、初回実行時はテーブル作成に時間がかかる場合があります
- 接続文字列のパラメータ名（Host, Port など）は大文字小文字を区別します

### テスト実行の注意点

- テスト間の独立性を保つため、各テストメソッドは独自のデータを作成するようにしてください
- テストフレームワークの並列実行設定によっては、ポート競合が発生する可能性があります
- テストがクリーンアップされずに終了した場合、手動でDockerコンテナを削除する必要がある場合があります

```bash
# 停止していないコンテナの確認
docker ps

# コンテナの停止と削除
docker stop <container_id>
docker rm <container_id>
```

---

このプロジェクトは、.NET Framework環境でのDockerコンテナを使用したデータベーステストの実装例として提供されています。実際のプロジェクトに適用する際は、セキュリティや運用面も考慮してカスタマイズしてください。