using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PipeMessenger.Pipes;
using Xunit;

namespace PipeMessenger.Test
{
    public class PipeTest
    {
        [Fact]
        public void IsConnected_GetsValueFromPipeStream()
        {
            // Arrange
            var pipeStreamMock = CreatePipeStreamMock();
            pipeStreamMock.SetupGet(pipeStream => pipeStream.IsConnected).Returns(true);
            var target = CreateAndInitPipe(pipeStreamMock.Object);

            // Act
            var isConnected = target.IsConnected;

            // Assert
            isConnected.Should().BeTrue();
            pipeStreamMock.VerifyGet(pipeStream => pipeStream.IsConnected, Times.Once());
        }

        private static Pipe CreatePipe(IPipeStream pipeStream)
        {
            return new Pipe(() => pipeStream);
        }

        private static Pipe CreateAndInitPipe(IPipeStream pipeStream)
        {
            var pipe = new Pipe(() => pipeStream);

            pipe.Init(() => {}, CancellationToken.None);

            return pipe;
        }

        private static Mock<IPipeStream> CreatePipeStreamMock()
        {
            var pipeStreamMock = new Mock<IPipeStream>();

            pipeStreamMock
                .Setup(pipeStream => pipeStream.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            pipeStreamMock
                .Setup(pipeStream => pipeStream.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(1));
            pipeStreamMock
                .Setup(pipeStream => pipeStream.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return pipeStreamMock;
        }
    }
}
