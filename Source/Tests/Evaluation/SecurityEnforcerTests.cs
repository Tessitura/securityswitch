// =================================================================================
// Copyright © 2004-2011 Matt Sollars
// All rights reserved.
// 
// This code and information is provided "as is" without warranty of any kind,
// either expressed or implied, including, but not limited to, the implied 
// warranties of merchantability and/or fitness for a particular purpose.
// =================================================================================
using System;

using Moq;

using SecuritySwitch.Abstractions;
using SecuritySwitch.Configuration;
using SecuritySwitch.Evaluation;

using Xunit;


namespace SecuritySwitch.Tests.Evaluation {
	public class SecurityEnforcerTests {
		[Fact]
		public void GetUriReuestReturnsNullIfRequestSecurityAlreadyMatchesSpecifiedSecurity() {
			// Arrange.
			var mockRequest = new Mock<HttpRequestBase>();
			var mockResponse = new Mock<HttpResponseBase>();
			var settings = new Settings();
			var enforcer = new SecurityEnforcer();

			// Act.
			mockRequest.SetupGet(req => req.IsSecureConnection).Returns(true);
			var targetUrlForAlreadySecuredRequest = enforcer.GetUriForMatchedSecurityRequest(mockRequest.Object,
			                                                                                 mockResponse.Object,
			                                                                                 RequestSecurity.Secure,
			                                                                                 settings);

			mockRequest.SetupGet(req => req.IsSecureConnection).Returns(false);
			var targetUrlForAlreadyInsecureRequest = enforcer.GetUriForMatchedSecurityRequest(mockRequest.Object,
			                                                                                  mockResponse.Object,
			                                                                                  RequestSecurity.Insecure,
			                                                                                  settings);


			// Assert.
			Assert.Null(targetUrlForAlreadySecuredRequest);
			Assert.Null(targetUrlForAlreadyInsecureRequest);
		}

		[Fact]
		public void GetUriReturnsTheRequestUrlWithProtocolReplacedWhenNoBaseUriIsSupplied() {
			// Arrange.
			const string BaseRequestUri = "http://www.testsite.com";
			const string PathRequestUri = "/Manage/Default.aspx?Param=SomeValue";

			var mockRequest = new Mock<HttpRequestBase>();
			mockRequest.SetupGet(req => req.Url).Returns(new Uri(BaseRequestUri + PathRequestUri));
			mockRequest.SetupGet(req => req.RawUrl).Returns(PathRequestUri);

			var mockResponse = new Mock<HttpResponseBase>();
			mockResponse.Setup(resp => resp.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

			var settings = new Settings {
				Mode = Mode.On,
				Paths = {
					new TestPathSetting("/Manage")
				}
			};
			var enforcer = new SecurityEnforcer();

			// Act.
			var targetUrl = enforcer.GetUriForMatchedSecurityRequest(mockRequest.Object,
			                                                         mockResponse.Object,
			                                                         RequestSecurity.Secure,
			                                                         settings);

			// Assert.
			Assert.Equal(BaseRequestUri.Replace("http://", "https://") + PathRequestUri, targetUrl);
		}

		[Fact]
		public void GetUriReturnsUriBasedOnSuppliedBaseSecureUri() {
			const string BaseRequestUri = "http://www.testsite.com";
			const string PathRequestUri = "/Manage/Default.aspx?Param=SomeValue";

			var mockRequest = new Mock<HttpRequestBase>();
			mockRequest.SetupGet(req => req.Url).Returns(new Uri(BaseRequestUri + PathRequestUri));
			mockRequest.SetupGet(req => req.RawUrl).Returns(PathRequestUri);

			var mockResponse = new Mock<HttpResponseBase>();
			mockResponse.Setup(resp => resp.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

			var settings = new Settings {
				Mode = Mode.On,
				BaseSecureUri = "https://secure.someotherwebsite.com/testsite/"
			};
			var enforcer = new SecurityEnforcer();

			// Act.
			var targetUrl = enforcer.GetUriForMatchedSecurityRequest(mockRequest.Object,
																	 mockResponse.Object,
																	 RequestSecurity.Secure,
																	 settings);

			// Assert.
			Assert.Equal(settings.BaseSecureUri + PathRequestUri.Remove(0, 1), targetUrl);
		}

		[Fact]
		public void GetUriReturnsUriBasedOnSuppliedBaseInsecureUri() {
			const string BaseRequestUri = "https://www.testsite.com";
			const string PathRequestUri = "/Info/Default.aspx?Param=SomeValue";

			var mockRequest = new Mock<HttpRequestBase>();
			mockRequest.SetupGet(req => req.Url).Returns(new Uri(BaseRequestUri + PathRequestUri));
			mockRequest.SetupGet(req => req.RawUrl).Returns(PathRequestUri);
			mockRequest.SetupGet(req => req.IsSecureConnection).Returns(true);

			var mockResponse = new Mock<HttpResponseBase>();
			mockResponse.Setup(resp => resp.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

			var settings = new Settings {
				Mode = Mode.On,
				BaseInsecureUri = "http://www.someotherwebsite.com/"
			};
			var enforcer = new SecurityEnforcer();

			// Act.
			var targetUrl = enforcer.GetUriForMatchedSecurityRequest(mockRequest.Object,
																	 mockResponse.Object,
																	 RequestSecurity.Insecure,
																	 settings);

			// Assert.
			Assert.Equal(settings.BaseInsecureUri + PathRequestUri.Remove(0, 1), targetUrl);
		}
	}
}