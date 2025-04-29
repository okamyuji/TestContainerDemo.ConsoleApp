using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TestContainerDemo.Tests.Helpers
{
    public class DockerContainerHelper : IDisposable
    {
        private readonly DockerClient _dockerClient;
        private string _containerId;
        private readonly string _imageName;
        private readonly List<string> _environmentVariables;
        private readonly Dictionary<string, string> _portMappings;
        private string _containerName;

        public DockerContainerHelper(string imageName)
        {
            _imageName = imageName;
            _environmentVariables = new List<string>();
            _portMappings = new Dictionary<string, string>();
            _containerName = $"container-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            // Dockerクライアントの初期化
            _dockerClient = new DockerClientConfiguration(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? new Uri("npipe://./pipe/docker_engine")
                        : new Uri("unix:///var/run/docker.sock"))
                .CreateClient();
        }

        public DockerContainerHelper WithName(string name)
        {
            _containerName = name;
            return this;
        }

        public DockerContainerHelper WithEnvironment(string key, string value)
        {
            _environmentVariables.Add($"{key}={value}");
            return this;
        }

        public DockerContainerHelper WithPortMapping(string hostPort, string containerPort)
        {
            _portMappings.Add(hostPort, containerPort);
            return this;
        }

        public async Task<string> StartAsync()
        {
            try
            {
                Console.WriteLine($"イメージ {_imageName} を使用してコンテナを作成します...");

                // 既存のイメージを使用するため、イメージのプル処理をスキップして直接コンテナ作成に進む

                // ポート設定
                Dictionary<string, IList<PortBinding>> portBindings = new Dictionary<string, IList<PortBinding>>();
                foreach (KeyValuePair<string, string> port in _portMappings)
                {
                    portBindings.Add(port.Value, new List<PortBinding>
            {
                new PortBinding
                {
                    HostPort = port.Key
                }
            });
                }

                // コンテナの作成
                Console.WriteLine($"コンテナ '{_containerName}' を作成しています...");
                CreateContainerResponse createResponse = await _dockerClient.Containers.CreateContainerAsync(
                    new CreateContainerParameters
                    {
                        Image = _imageName,
                        Name = _containerName,
                        Env = _environmentVariables,
                        ExposedPorts = portBindings.Keys.ToDictionary(k => k, v => new EmptyStruct()),
                        HostConfig = new HostConfig
                        {
                            PortBindings = portBindings
                        }
                    });

                _containerId = createResponse.ID;
                Console.WriteLine($"コンテナ ID: {_containerId} が作成されました。");

                // コンテナの起動
                Console.WriteLine($"コンテナ '{_containerName}' を起動しています...");
                await _dockerClient.Containers.StartContainerAsync(
                    _containerId,
                    new ContainerStartParameters());

                Console.WriteLine($"コンテナ '{_containerName}' が正常に起動しました。");
                return _containerId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"コンテナの起動中にエラーが発生しました: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部エラー: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<string> GetContainerIpAsync()
        {
            ContainerInspectResponse inspectResponse = await _dockerClient.Containers.InspectContainerAsync(_containerId);
            return inspectResponse.NetworkSettings.IPAddress;
        }

        public async Task StopAsync()
        {
            if (!string.IsNullOrEmpty(_containerId))
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(
                        _containerId,
                        new ContainerStopParameters
                        {
                            WaitBeforeKillSeconds = 10
                        });

                    await _dockerClient.Containers.RemoveContainerAsync(
                        _containerId,
                        new ContainerRemoveParameters
                        {
                            Force = true
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"コンテナの停止中にエラーが発生しました: {ex.Message}");
                }
                finally
                {
                    _containerId = null;
                }
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
            _dockerClient?.Dispose();
        }
    }
}