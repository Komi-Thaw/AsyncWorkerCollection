using System;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class DoubleBufferTest
    {
        [ContractTestCase]
        public void DoAll()
        {
            "���߳�����ӳ�һ�߼���Ԫ��һ��ִ�У�����ִ������Ԫ��".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var random = new Random();
                const int n = 100;

                var doubleBuffer = new DoubleBuffer<IFoo>();

                var t1 = Task.Run(async () =>
                {
                    for (int i = 0; i < n; i++)
                    {
                        doubleBuffer.Add(mock.Object);
                        await Task.Delay(random.Next(100));
                    }
                });

                var t2 = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    await doubleBuffer.DoAllAsync(async list =>
                    {
                        foreach (var foo in list)
                        {
                            await Task.Delay(random.Next(50));
                            foo.Foo();
                        }
                    });
                });

                Task.WaitAll(t1, t2);

                doubleBuffer.DoAllAsync(async list =>
                {
                    foreach (var foo in list)
                    {
                        await Task.Delay(random.Next(50));
                        foo.Foo();
                    }
                }).Wait();

                mock.Verify(foo => foo.Foo(), Times.Exactly(n));
            });

            "���߳�һ�߼���Ԫ��һ��ִ�У�����ִ������Ԫ��".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                const int n = 10000;

                var doubleBuffer = new DoubleBuffer<IFoo>();

                for (int i = 0; i < n; i++)
                {
                    doubleBuffer.Add(mock.Object);
                }

                var t1 = Task.Run(() =>
                {
                    for (int i = 0; i < n; i++)
                    {
                        doubleBuffer.Add(mock.Object);
                    }
                });

                var t2 = Task.Run(() => { doubleBuffer.DoAll(list => list.ForEach(foo => foo.Foo())); });

                Task.WaitAll(t1, t2);

                // û��ִ��һ��
                mock.Verify(foo => foo.Foo(), Times.Exactly(n * 2));
            });

            "����10��Ԫ�أ�ִ�� DoAll Ԫ��ִ��10��".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                const int n = 10;

                var doubleBuffer = new DoubleBuffer<IFoo>();

                for (int i = 0; i < n; i++)
                {
                    doubleBuffer.Add(mock.Object);
                }

                doubleBuffer.DoAll(list => list.ForEach(foo => foo.Foo()));

                // û��ִ��һ��
                mock.Verify(foo => foo.Foo(), Times.Exactly(n));
            });

            "û�и����������ݣ�ִ�� DoAll ɶ������".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var doubleBuffer = new DoubleBuffer<IFoo>();
                doubleBuffer.DoAll(list => list.ForEach(foo => foo.Foo()));

                // û��ִ��һ��
                mock.Verify(foo => foo.Foo(), Times.Never);
            });
        }

        public interface IFoo
        {
            void Foo();
        }
    }
}