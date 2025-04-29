# TestContainerDemo

TestContainerDemo�v���W�F�N�g�́A.NET Framework 4.8.1���g�p����Docker�R���e�i���Oracle�����PostgreSQL�f�[�^�x�[�X��CRUD��������s���A�e�X�g���s�����߂̃f���A�v���P�[�V�����ł��B

## ���v��

���̃v���W�F�N�g�����s����ɂ́A�ȉ��̊����K�v�ł�

- .NET Framework 4.8.1
- Visual Studio 2019�܂���2022
- Docker Desktop
- Docker��̃f�[�^�x�[�X�C���[�W:
  - PostgreSQL (`postgres:latest`)
  - Oracle (`gvenzl/oracle-xe:latest`)

## �v���W�F�N�g�\��

�v���W�F�N�g�\���͈ȉ��̒ʂ�ł�

```
TestContainerDemo/
������ TestContainerDemo.sln
������ TestContainerDemo.ConsoleApp/
��   ������ Models/
��   ��   ������ Customer.cs
��   ������ Repositories/
��   ��   ������ ICustomerRepository.cs
��   ��   ������ OracleCustomerRepository.cs
��   ��   ������ PostgresCustomerRepository.cs
��   ������ Program.cs
������ TestContainerDemo.Tests/
    ������ Helpers/
    ��   ������ DockerContainerHelper.cs
    ������ OracleCustomerRepositoryTests.cs
    ������ PostgresCustomerRepositoryTests.cs
```

## �Z�b�g�A�b�v�菇

### 1. �v���W�F�N�g�̃Z�b�g�A�b�v

1. ���|�W�g�����N���[���܂��̓_�E�����[�h���܂�
2. Visual Studio�Ń\�����[�V�����i`TestContainerDemo.sln`�j���J���܂�
3. NuGet�p�b�P�[�W�̕������s���܂�

### 2. �K�v��NuGet�p�b�P�[�W

�R���\�[���A�v���P�[�V�����v���W�F�N�g�ɂ͈ȉ��̃p�b�P�[�W���C���X�g�[�����܂�

```
Install-Package Oracle.ManagedDataAccess -Version 23.8.0
Install-Package Npgsql -Version 8.0.7 # �ŐV��9.0.3��.NET Framework 4.8.1�ł͎g�p�ł��܂���
```

�e�X�g�v���W�F�N�g�ɂ͈ȉ��̃p�b�P�[�W���C���X�g�[�����܂��F

```
Install-Package MSTest.TestAdapter -Version 3.8.3
Install-Package MSTest.TestFramework -Version 3.8.3
Install-Package Docker.DotNet -Version 3.125.15
```

### 3. Docker�C���[�W�̃v��

�K�v��Docker�C���[�W���v�����܂�

```bash
docker pull postgres:latest
docker pull gvenzl/oracle-xe:latest
```

## Docker Test Containers�̎���

�{�v���W�F�N�g�ł́A�e�X�g����Docker�R���e�i�𓮓I�ɋN���E��~����@�\���������Ă��܂��B�ȉ��͎�v�ȋ@�\�ł�

### DockerContainerHelper�N���X

`DockerContainerHelper`�N���X��Docker�R���e�i�̊Ǘ��𒊏ۉ����A�ȉ��̋@�\��񋟂��܂�

- �R���e�i�̍쐬�ƋN��
- ���ϐ��̐ݒ�
- �|�[�g�}�b�s���O�̐ݒ�
- �R���e�i�̒�~�ƍ폜

```csharp
// �g�p��
var container = new DockerContainerHelper("postgres:latest")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithPortMapping("15432", "5432/tcp");

await container.StartAsync();
// ... �R���e�i���g�p�������� ...
await container.StopAsync();
```

## ���|�W�g���p�^�[���̎���

�{�v���W�F�N�g�ł̓��|�W�g���p�^�[�����̗p���Ă��܂�

### ICustomerRepository�C���^�[�t�F�[�X

�f�[�^�A�N�Z�X�𒊏ۉ�����C���^�[�t�F�[�X�ŁA�ȉ���CRUD������`���Ă��܂��F

- `CreateCustomerAsync` - �ڋq���̓o�^
- `GetCustomerByIdAsync` - ID�w��ɂ��ڋq���̎擾
- `GetAllCustomersAsync` - �S�ڋq���̎擾
- `UpdateCustomerAsync` - �ڋq���̍X�V
- `DeleteCustomerAsync` - �ڋq���̍폜

���̃C���^�[�t�F�[�X�ɂ��A�f�[�^�x�[�X�����̏ڍׂ��B�����A�قȂ�f�[�^�x�[�X�ɑ΂��ē���̃C���^�[�t�F�[�X�ŃA�N�Z�X�ł��܂��B

## Oracle�����̓���

`OracleCustomerRepository`�N���X�́AOracle Database�ŗL�̎�����񋟂��܂�

### ��ȓ���

- **Oracle.ManagedDataAccess**���C�u�����̎g�p
- �V�[�P���X�ƃg���K�[�ɂ��ID��������
- PL/SQL���g�p�����e�[�u������������
- ���O�t���p�����[�^�i`:parameter`�`���j�̎g�p
- OUT�p�����[�^���g�p��������ID�̎擾

### Oracle�ŗL��SQL��

```sql
-- �V�[�P���X�쐬
CREATE SEQUENCE CUSTOMERS_SEQ START WITH 1 INCREMENT BY 1

-- �g���K�[�ɂ��ID��������
CREATE OR REPLACE TRIGGER CUSTOMERS_BI_TRG 
BEFORE INSERT ON CUSTOMERS 
FOR EACH ROW 
BEGIN 
    IF :NEW.ID IS NULL THEN 
        SELECT CUSTOMERS_SEQ.NEXTVAL INTO :NEW.ID FROM DUAL; 
    END IF; 
END;
```

## PostgreSQL�����̓���

`PostgresCustomerRepository`�N���X�́APostgreSQL�ŗL�̎�����񋟂��܂�

### ��ȓ���

- **Npgsql**���C�u�����̎g�p
- SERIAL�^�ɂ��ID��������
- �p�����[�^�w��i`@parameter`�`���j�̎g�p
- RETURNING��ɂ�鐶��ID�̒��ڎ擾

### PostgreSQL�ŗL��SQL��

```sql
-- SERIAL�^�ɂ��ID�����A��
CREATE TABLE IF NOT EXISTS customers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    created_at TIMESTAMP NOT NULL
)

-- RETURNING��ɂ�鐶��ID�̎擾
INSERT INTO customers (name, email, created_at) 
VALUES (@Name, @Email, @CreatedAt) 
RETURNING id
```

## �e�X�g����

�{�v���W�F�N�g�́AMSTest�t���[�����[�N���g�p���Ċe���|�W�g���̎������e�X�g���Ă��܂��B

### �e�X�g�̓���

- Docker��̃f�[�^�x�[�X�R���e�i�𓮓I�ɋN���E��~
- ��ӂ̃R���e�i���ɂ�镪��
- ��p�̃|�[�g�}�b�s���O�ɂ��|�[�g�����̉��
- �e�X�g��̃��\�[�X����i�R���e�i�̒�~�ƍ폜�j

### �e�X�g���s�̗���

1. �e�X�g�N���X�̏��������i`ClassInitialize`�j��Docker�R���e�i���N��
2. �f�[�^�x�[�X�̏����i�e�[�u���쐬�Ȃǁj
3. �e�e�X�g���\�b�h�̎��s
4. �e�X�g�N���X�̃N���[���A�b�v�i`ClassCleanup`�j�ŃR���e�i���~�E�폜

## �g�p��

### �R���\�[���A�v���P�[�V�����̎g�p

```csharp
// ���ϐ��Őڑ��������ݒ�
Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", 
    "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres");

// PostgreSQL���|�W�g�����g�p������
var repository = new PostgresCustomerRepository(
    Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"));

// �e�[�u���̏���
await repository.EnsureTableCreatedAsync();

// �ڋq���̓o�^
var customer = new Customer
{
    Name = "�R�c���Y",
    Email = "taro.yamada@example.com",
    CreatedAt = DateTime.Now
};

var newId = await repository.CreateCustomerAsync(customer);
Console.WriteLine($"�o�^���ꂽID: {newId}");

// �ڋq���̎擾
var retrievedCustomer = await repository.GetCustomerByIdAsync(newId);
Console.WriteLine($"�擾�����ڋq���: {retrievedCustomer}");
```

### �e�X�g�̎��s

1. Visual Studio�̃e�X�g�G�N�X�v���[���[����e�X�g�����s
2. �܂��́A�R�}���h���C������`VSTest.Console.exe`���g�p���Ď��s

```
VSTest.Console.exe TestContainerDemo.Tests.dll
```

## ���ӓ_

### Docker�̗��p

- Docker���N�����Ă��邱�Ƃ��m�F���Ă�������
- �K�v��Docker�C���[�W���v������Ă��邱�Ƃ��m�F���Ă�������
- �|�[�g��������������ꍇ�́A�g�p����|�[�g�ԍ���ύX���Ă�������

### Oracle�֘A�̒��ӓ_

- Oracle�R���e�i�͋N���Ɏ��Ԃ�������ꍇ������܂�
- Oracle��XE�G�f�B�V�����̓������g�p�ʂ��������߁A�V�X�e�����\�[�X�ɒ��ӂ��Ă�������
- �ڑ�������̌`���ɒ��ӂ��Ă��������i���ɃT�[�r�X���̕����j

### PostgreSQL�֘A�̒��ӓ_

- PostgreSQL�R���e�i��Oracle�Ɣ�r���Čy�ʂł����A������s���̓e�[�u���쐬�Ɏ��Ԃ�������ꍇ������܂�
- �ڑ�������̃p�����[�^���iHost, Port �Ȃǁj�͑啶������������ʂ��܂�

### �e�X�g���s�̒��ӓ_

- �e�X�g�Ԃ̓Ɨ�����ۂ��߁A�e�e�X�g���\�b�h�͓Ǝ��̃f�[�^���쐬����悤�ɂ��Ă�������
- �e�X�g�t���[�����[�N�̕�����s�ݒ�ɂ���ẮA�|�[�g��������������\��������܂�
- �e�X�g���N���[���A�b�v���ꂸ�ɏI�������ꍇ�A�蓮��Docker�R���e�i���폜����K�v������ꍇ������܂�

```bash
# ��~���Ă��Ȃ��R���e�i�̊m�F
docker ps

# �R���e�i�̒�~�ƍ폜
docker stop <container_id>
docker rm <container_id>
```

---

���̃v���W�F�N�g�́A.NET Framework���ł�Docker�R���e�i���g�p�����f�[�^�x�[�X�e�X�g�̎�����Ƃ��Ē񋟂���Ă��܂��B���ۂ̃v���W�F�N�g�ɓK�p����ۂ́A�Z�L�����e�B��^�p�ʂ��l�����ăJ�X�^�}�C�Y���Ă��������B