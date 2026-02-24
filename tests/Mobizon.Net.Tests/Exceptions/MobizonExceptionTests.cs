using System;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models;
using Xunit;

namespace Mobizon.Net.Tests.Exceptions
{
    public class MobizonExceptionTests
    {
        [Fact]
        public void MobizonException_WithMessage_SetsMessage()
        {
            var ex = new MobizonException("test error");
            Assert.Equal("test error", ex.Message);
        }

        [Fact]
        public void MobizonException_WithInnerException_SetsInner()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new MobizonException("outer", inner);

            Assert.Equal("outer", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void MobizonApiException_SetsCodeAndMessage()
        {
            var ex = new MobizonApiException(2, "Auth failed");

            Assert.Equal(MobizonResponseCode.AuthFailed, ex.Code);
            Assert.Equal(2, ex.RawCode);
            Assert.Equal("Auth failed", ex.ApiMessage);
            Assert.Contains("2", ex.Message);
            Assert.Contains("Auth failed", ex.Message);
        }

        [Fact]
        public void MobizonApiException_UnknownCode_PreservesRawCode()
        {
            var ex = new MobizonApiException(999, "Unknown");

            Assert.Equal(999, ex.RawCode);
            Assert.Equal((MobizonResponseCode)999, ex.Code);
        }

        [Fact]
        public void MobizonApiException_IsSubclassOfMobizonException()
        {
            var ex = new MobizonApiException(1, "test");
            Assert.IsAssignableFrom<MobizonException>(ex);
        }
    }
}
