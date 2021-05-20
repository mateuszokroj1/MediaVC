﻿using System;
using System.IO;

using Xunit;

namespace MediaVC.Tools.Tests.Detection.TextDetector.Strategies.StreamTextDetectionStrategy
{
    public class Constructor
    {
        [Fact]
        public void Constructor_WhenArgumentIsNull_ShouldThrowException()
        {
            Stream stream = null;

            Assert.Throws<ArgumentNullException>(() => new Tools.Detection.Strategies.StreamTextDetectionStrategy(stream));
        }

        [Fact]
        public void Constructor_WhenArgumentIsValid_ShouldSetProperty()
        {
            var stream = Stream.Null;

            var result = new Tools.Detection.Strategies.StreamTextDetectionStrategy(stream);

            Assert.Equal(stream, result.Stream);
        }
    }
}
