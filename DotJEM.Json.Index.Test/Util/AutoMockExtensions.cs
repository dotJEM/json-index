using System;
using System.Linq.Expressions;
using Moq.AutoMock;
using Moq.Language.Flow;

namespace DotJEM.Json.Index.Test.Util
{
    public static class AutoMockExtensions
    {
        public static ISetup<TFake, object> Setup<TFake>(this AutoMocker mocker, Expression<Func<TFake, object>> expression) where TFake : class => mocker.GetMock<TFake>().Setup(expression);
        public static ISetup<TFake> Setup<TFake>(this AutoMocker mocker, Expression<Action<TFake>> expression) where TFake : class => mocker.GetMock<TFake>().Setup(expression);
    }
}