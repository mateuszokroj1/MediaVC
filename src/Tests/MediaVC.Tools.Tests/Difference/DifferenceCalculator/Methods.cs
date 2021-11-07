﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using MediaVC.Difference;

using Moq;

using Xunit;

namespace MediaVC.Tools.Tests.Difference.DifferenceCalculator
{
    public class Methods : IClassFixture<DifferenceCalculatorTestFixture>
    {
        #region Constructor

        public Methods(DifferenceCalculatorTestFixture fixture)
        {
            this.fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        #endregion

        #region Fields

        private readonly IDifferenceCalculatorTestFixture fixture;

        #endregion

        #region Tests

        [Fact]
        public async Task Calculate_WhenCancellationRequested()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.OneZero);
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => calculator.CalculateAsync(cancellationSource.Token).AsTask());
        }

        [Fact]
        public async Task Calculate_WhenNewFile_Variant1_ShouldReturnOneSegment()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.OneZero);

            await calculator.CalculateAsync();

            Assert.Null(calculator.CurrentVersion);
            Assert.Equal(this.fixture.OneZero, calculator.NewVersion);

            Assert.NotNull(calculator.Result);
            var first = Assert.Single(calculator.Result);
            
            Assert.Equal(this.fixture.OneZero, first.Source);
            Assert.Equal(0, first.StartPositionInSource);
            Assert.Equal(0, first.EndPositionInSource);
            Assert.Equal(1U, first.Length);

            Assert.NotNull(calculator.Removed);
            Assert.Empty(calculator.Removed);
        }

        [Fact]
        public async Task Calculate_WhenNewFile_Variant2_ShouldReturnOneSegment()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.ThousandFullBytes);

            await calculator.CalculateAsync();

            Assert.Null(calculator.CurrentVersion);
            Assert.Equal(this.fixture.ThousandFullBytes, calculator.NewVersion);

            Assert.NotNull(calculator.Result);
            var first = Assert.Single(calculator.Result);

            Assert.Equal(this.fixture.ThousandFullBytes, first.Source);
            Assert.Equal(0, first.StartPositionInSource);
            Assert.Equal(this.fixture.ThousandFullBytes.Length - 1, first.EndPositionInSource);
            Assert.Equal((ulong)this.fixture.ThousandFullBytes.Length, first.Length);

            Assert.NotNull(calculator.Removed);
            Assert.Empty(calculator.Removed);
        }

        [Fact]
        public async Task Calculate_WhenNewFile_Variant3_ShouldReturnOneSegment()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(InputSource.Empty, this.fixture.OneZero);

            await calculator.CalculateAsync();

            Assert.Equal(InputSource.Empty, calculator.CurrentVersion);
            Assert.Equal(this.fixture.OneZero, calculator.NewVersion);

            Assert.NotNull(calculator.Result);
            var first = Assert.Single(calculator.Result);

            Assert.Equal(this.fixture.OneZero, first.Source);
            Assert.Equal(0, first.StartPositionInSource);
            Assert.Equal(this.fixture.OneZero.Length - 1, first.EndPositionInSource);
            Assert.Equal(this.fixture.OneZero.Length, (long)first.Length);

            Assert.NotNull(calculator.Removed);
            Assert.Empty(calculator.Removed);
        }

        [Fact]
        public async Task Calculate_WhenVersionEqual_Variant1_ShouldReturnOneSegment()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.OneZero, this.fixture.OneZero);
                
            await calculator.CalculateAsync();

            Assert.Equal(this.fixture.OneZero, calculator.CurrentVersion);
            Assert.Equal(this.fixture.OneZero, calculator.NewVersion);

            Assert.NotNull(calculator.Result);
            var result = Assert.Single(calculator.Result);

            Assert.Equal(this.fixture.OneZero, result.Source);
            Assert.Equal(0, result.StartPositionInSource);
            Assert.Equal(0, result.EndPositionInSource);
            Assert.Equal((ulong)1, result.Length);
            Assert.Equal(0, result.MappedPosition);

            Assert.NotNull(calculator.Removed);
            Assert.Empty(calculator.Removed);
        }

        [Fact]
        public async Task Calculate_WhenVersionEqual_Variant2_ShouldReturnOneSegment()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.ThousandFullBytes, this.fixture.ThousandFullBytes);

            await calculator.CalculateAsync();

            Assert.Equal(this.fixture.ThousandFullBytes, calculator.CurrentVersion);
            Assert.Equal(this.fixture.ThousandFullBytes, calculator.NewVersion);

            Assert.NotNull(calculator.Result);
            var result = Assert.Single(calculator.Result);

            Assert.Equal(this.fixture.ThousandFullBytes, result.Source);
            Assert.Equal(0, result.StartPositionInSource);
            Assert.Equal(this.fixture.ThousandFullBytes.Length - 1, result.EndPositionInSource);
            Assert.Equal((ulong)this.fixture.ThousandFullBytes.Length, result.Length);
            Assert.Equal(0, result.MappedPosition);

            Assert.NotNull(calculator.Removed);
            Assert.Empty(calculator.Removed);
        }

        [Fact]
        public async Task Calculate_WhenFileCleared_Variant1_ShouldReturnOneSegment()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.ThousandFullBytes, InputSource.Empty);

            await calculator.CalculateAsync();

            Assert.NotNull(calculator.Result);
            Assert.Empty(calculator.Result);

            Assert.NotNull(calculator.Removed);
            var result = Assert.Single(calculator.Removed);

            Assert.Equal(this.fixture.ThousandFullBytes, result.Source);
            Assert.Equal(0, result.StartPositionInSource);
            Assert.Equal(this.fixture.ThousandFullBytes.Length - 1, result.EndPositionInSource);
            Assert.Equal((ulong)this.fixture.ThousandFullBytes.Length, result.Length);
        }

        [Fact]
        public async Task Calculate_WhenFileIsDifferent_Variant1()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.ExampleSources[0], this.fixture.ExampleSources[1]);

            var observer1Mock = new Mock<IObserver<Unit>>(MockBehavior.Loose);
            observer1Mock.Setup(mocked => mocked.OnNext(It.IsAny<Unit>())).Verifiable();

            var observer2Mock = new Mock<IObserver<IFileSegmentInfo>>(MockBehavior.Loose);
            observer2Mock.Setup(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>())).Verifiable();

            using(calculator.Result.Cleared.Subscribe(observer1Mock.Object))
            {
                using(calculator.Result.Added.Subscribe(observer2Mock.Object))
                {
                    await calculator.CalculateAsync();

                    Assert.Equal(this.fixture.ExampleSources[0], calculator.CurrentVersion);
                    Assert.Equal(this.fixture.ExampleSources[1], calculator.NewVersion);

                    Assert.NotNull(calculator.Result);
                    Assert.Equal(2, calculator.Result.Count());

                    var result = calculator.Result.ElementAt(0);
                    Assert.Equal(this.fixture.ExampleSources[0], result.Source);
                    Assert.Equal(0, result.StartPositionInSource);
                    Assert.Equal(3L, result.EndPositionInSource);
                    Assert.Equal(4L, (long)result.Length);

                    result = calculator.Result.ElementAt(1);
                    Assert.Equal(this.fixture.ExampleSources[1], result.Source);
                    Assert.Equal(4L, result.StartPositionInSource);
                    Assert.Equal(7L, result.EndPositionInSource);
                    Assert.Equal(4L, (long)result.Length);

                    Assert.NotNull(calculator.Removed);
                    Assert.Empty(calculator.Removed);

                    observer1Mock.Verify(mocked => mocked.OnNext(It.IsAny<Unit>()));
                    observer2Mock.Verify(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>()), Times.Exactly(2));
                }
            }
        }

        [Fact]
        public async Task Calculate_WhenFileIsDifferent_Variant2()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.ExampleSources[0], this.fixture.ExampleSources[2]);

            var observer1Mock = new Mock<IObserver<Unit>>(MockBehavior.Loose);
            observer1Mock.Setup(mocked => mocked.OnNext(It.IsAny<Unit>())).Verifiable();

            var observer2Mock = new Mock<IObserver<IFileSegmentInfo>>(MockBehavior.Loose);
            observer2Mock.Setup(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>())).Verifiable();

            using(calculator.Result.Cleared.Subscribe(observer1Mock.Object))
            {
                using(calculator.Result.Added.Subscribe(observer2Mock.Object))
                {
                    await calculator.CalculateAsync();

                    Assert.Equal(this.fixture.ExampleSources[0], calculator.CurrentVersion);
                    Assert.Equal(this.fixture.ExampleSources[2], calculator.NewVersion);

                    Assert.NotNull(calculator.Result);
                    Assert.Equal(2, calculator.Result.Count());

                    var result = calculator.Result.ElementAt(0);
                    Assert.Equal(this.fixture.ExampleSources[2], result.Source);
                    Assert.Equal(0L, result.StartPositionInSource);
                    Assert.Equal(3L, result.EndPositionInSource);
                    Assert.Equal(0, result.MappedPosition);
                    Assert.Equal(4L, (long)result.Length);

                    result = calculator.Result.ElementAt(1);
                    Assert.Equal(this.fixture.ExampleSources[0], result.Source);
                    Assert.Equal(0, result.StartPositionInSource);
                    Assert.Equal(3L, result.EndPositionInSource);
                    Assert.Equal(4L, result.MappedPosition);
                    Assert.Equal(4L, (long)result.Length);

                    Assert.NotNull(calculator.Removed);
                    Assert.Empty(calculator.Removed);

                    observer1Mock.Verify(mocked => mocked.OnNext(It.IsAny<Unit>()));
                    observer2Mock.Verify(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>()), Times.Exactly(2));
                }
            }
        }

        [Fact]
        public async Task Calculate_WhenFileIsDifferent_Variant3()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.ExampleSources[1], this.fixture.ExampleSources[2]);

            var observer1Mock = new Mock<IObserver<Unit>>(MockBehavior.Loose);
            observer1Mock.Setup(mocked => mocked.OnNext(It.IsAny<Unit>())).Verifiable();

            var observer2Mock = new Mock<IObserver<IFileSegmentInfo>>(MockBehavior.Loose);
            observer2Mock.Setup(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>())).Verifiable();

            using(calculator.Result.Cleared.Subscribe(observer1Mock.Object))
            {
                using(calculator.Result.Added.Subscribe(observer2Mock.Object))
                {
                    await calculator.CalculateAsync();

                    Assert.Equal(this.fixture.ExampleSources[1], calculator.CurrentVersion);
                    Assert.Equal(this.fixture.ExampleSources[2], calculator.NewVersion);

                    Assert.NotNull(calculator.Result);
                    Assert.Equal(2, calculator.Result.Count());

                    var result = calculator.Result.ElementAt(0);
                    Assert.Equal(this.fixture.ExampleSources[2], result.Source);
                    Assert.Equal(0L, result.StartPositionInSource);
                    Assert.Equal(3L, result.EndPositionInSource);
                    Assert.Equal(0, result.MappedPosition);
                    Assert.Equal(4L, (long)result.Length);

                    result = calculator.Result.ElementAt(1);
                    Assert.Equal(this.fixture.ExampleSources[1], result.Source);
                    Assert.Equal(0L, result.StartPositionInSource);
                    Assert.Equal(3L, result.EndPositionInSource);
                    Assert.Equal(4L, result.MappedPosition);
                    Assert.Equal(4L, (long)result.Length);

                    Assert.NotNull(calculator.Removed);

                    result = Assert.Single(calculator.Removed);
                    Assert.Equal(this.fixture.ExampleSources[1], result.Source);
                    Assert.Equal(4L, result.StartPositionInSource);
                    Assert.Equal(7L, result.EndPositionInSource);
                    Assert.Equal(4L, (long)result.Length);

                    observer1Mock.Verify(mocked => mocked.OnNext(It.IsAny<Unit>()));
                    observer2Mock.Verify(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>()), Times.Exactly(2));
                }
            }
        }

        [Fact]
        public async Task Calculate_WhenFileIsDifferent_Variant4()
        {
            var calculator = new Tools.Difference.DifferenceCalculator(this.fixture.ExampleSources[1], this.fixture.ExampleSources[3]);

            var observer1Mock = new Mock<IObserver<Unit>>(MockBehavior.Loose);
            observer1Mock.Setup(mocked => mocked.OnNext(It.IsAny<Unit>())).Verifiable();

            var observer2Mock = new Mock<IObserver<IFileSegmentInfo>>(MockBehavior.Loose);
            observer2Mock.Setup(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>())).Verifiable();

            using(calculator.Result.Cleared.Subscribe(observer1Mock.Object))
            {
                using(calculator.Result.Added.Subscribe(observer2Mock.Object))
                {
                    await calculator.CalculateAsync();

                    Assert.Equal(this.fixture.ExampleSources[1], calculator.CurrentVersion);
                    Assert.Equal(this.fixture.ExampleSources[3], calculator.NewVersion);

                    Assert.NotNull(calculator.Result);
                    //Assert.Equal(1, calculator.Result.Count);

                    /*var result = calculator.Result[0];

                    Assert.Equal(this.fixture.ExampleSources[2], result.Source);
                    Assert.Equal(0L, result.StartPosition);
                    Assert.Equal(7L, result.EndPosition);
                    Assert.Equal(8L, (long)result.Length);*/

                    Assert.NotNull(calculator.Removed);
                    Assert.Single(calculator.Removed);

                    observer1Mock.Verify(mocked => mocked.OnNext(It.IsAny<Unit>()));
                    observer2Mock.Verify(mocked => mocked.OnNext(It.IsAny<IFileSegmentInfo>()), Times.Exactly(3));
                }
            }
        }

        #endregion
    }
}