namespace DITest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    class Program
    {
        public enum ScopeType
        {
            Singleton = 0,
            Transient = 1,
        };


        private static class DI
        {
            private static DIContainer _container;

            public static void Initialize(DIContainer container)
            {
                _container = container;
            }

            public static T GetService<T>()
            {
                return _container.GetService<T>();
            }
        };


        private class DIContainer
        {
            public class ServiceItem
            {
                public ScopeType ScopeType { get; set; }
                public Type Type { get; set; }
                public Type ImplementationType { get; set; }
                public Func<object> ImplementationFactory { get; set; }

            }


            private Dictionary<Guid, ServiceItem> _services = new Dictionary<Guid, ServiceItem>();

            private Dictionary<Guid, object> _singletonServices = new Dictionary<Guid, object>();


            public T GetService<T>()
            {
                Guid typeID = GetTypeID<T>();

                var serviceItem = _services[typeID];


                if (serviceItem.ScopeType == ScopeType.Singleton)
                {
                    var instance = _singletonServices[typeID];

                    if (instance == null)
                    {
                        var instanceFactory = serviceItem.ImplementationFactory;


                        if (instanceFactory != null)
                            instance = instanceFactory.Invoke();
                        else
                            // Constraining T to new() might not be a good idea since some "constructor-less" 
                            // types have to be called through GetService<T>() 
                            instance = Activator.CreateInstance(serviceItem.ImplementationType);

                        _singletonServices[typeID] = instance;
                    };

                    return (T)instance;
                };


                if (serviceItem.ScopeType == ScopeType.Transient)
                {
                    var instance = serviceItem.ImplementationFactory();

                    return (T)instance;
                };


                throw new Exception("How");
            }



            public void AddSingleton<T>()
                where T : class, new()
            {
                Guid typeID = GetTypeID<T>();

                _services.Add(typeID, new ServiceItem()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(T),
                    ScopeType = ScopeType.Singleton,
                });

                _singletonServices.Add(typeID, null);
            }

            public void AddSingleton<T>(Func<T> implementationFactory)
                where T : class
            {
                Guid typeID = GetTypeID<T>();

                _services.Add(typeID, new ServiceItem()
                {
                    Type = typeof(T),
                    ScopeType = ScopeType.Singleton,
                    ImplementationFactory = () => implementationFactory(),
                });

                _singletonServices.Add(typeID, null);
            }


            public void AddSingelton<T, TImplementation>()
                where T : class
            {
                Guid typeID = GetTypeID<T>();

                _services.Add(typeID, new ServiceItem()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(TImplementation),
                    ScopeType = ScopeType.Singleton,
                });

                _singletonServices.Add(typeID, null);
            }

            public void AddSingleton<T, TImplementation>(Func<TImplementation> implementationFactory)
                where T : class
                where TImplementation : T
            {
                Guid typeID = GetTypeID<T>();

                _services.Add(typeID, new ServiceItem()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(TImplementation),
                    ScopeType = ScopeType.Singleton,
                    ImplementationFactory = () => implementationFactory(),
                });

                _singletonServices.Add(typeID, null);
            }



            public void AddTransient<T>()
                where T : class, new()
            {
                Guid typeID = GetTypeID<T>();

                _services.Add(typeID, new ServiceItem()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(T),
                    ScopeType = ScopeType.Transient,

                    ImplementationFactory = () => { return new T(); },
                });

                // _transientServices.Add(typeID, null);
            }

            public void AddTransient<T, TImplementation>(Func<TImplementation> implementationFactory)
                where T : class
                where TImplementation : T
            {
                Guid typeID = GetTypeID<T>();

                _services.Add(typeID, new ServiceItem()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(T),
                    ScopeType = ScopeType.Transient,

                    ImplementationFactory = () => implementationFactory(),
                });

                // _transientServices.Add(typeID, null);
            }


            private Guid GetTypeID<T>()
            {
                return typeof(T).GUID;
            }

        };


        public interface ITestClassSingelton
        {
            public string GUID { get; set; }
        }

        public class TestClassSingelton : ITestClassSingelton
        {
            private TestClass2Singelton _testClass2;
            public string GUID { get; set; } = Guid.NewGuid().ToString("N");


            public TestClassSingelton()
            {
                _testClass2 = DI.GetService<TestClass2Singelton>();
            }

        }
        public class TestClass2Singelton
        {
            private TestClassSingelton _testClass;

            public string GUID { get; } = Guid.NewGuid().ToString("N");


            public TestClass2Singelton()
            {
                // _testClass = DI.GetService<TestClass>();
            }
        };

        public class TestClass3SingeltonCtor
        {
            private TestClass2Singelton _testClass2;
            public string GUID { get; set; } = Guid.NewGuid().ToString("N");


            public TestClass3SingeltonCtor(string guid)
            {
                GUID = guid;
                _testClass2 = DI.GetService<TestClass2Singelton>();
            }

        }


        public interface ITestClassTransient
        {
            public string GUID { get; set; }
        }

        public class TestClassTransient : ITestClassTransient
        {
            private TestClass2Singelton _testClass2;
            public string GUID { get; set; } = Guid.NewGuid().ToString("N");


            public TestClassTransient()
            {
                _testClass2 = DI.GetService<TestClass2Singelton>();
            }

        }

        public interface ITestClassTransient2
        {
            public string GUID { get;  }
        }

        public class TestClass2Transient : ITestClassTransient2
        {
            private TestClassSingelton _testClass;

            public string GUID { get; } = Guid.NewGuid().ToString("N");
            
            public TestClass2Transient(string guid)
            {
                GUID = guid;
            }
        }


        static void Main(string[] args)
        {
            DIContainer di = new DIContainer();

            di.AddSingleton<TestClass2Singelton>();
            di.AddSingleton<TestClassSingelton>(() =>
            {
                return new TestClassSingelton()
                {
                    GUID = "2",
                };
            });
            di.AddSingleton<ITestClassSingelton, TestClassSingelton>(() =>
            {
                return new TestClassSingelton()
                {
                    GUID = "1",
                };
            });
            di.AddSingleton<TestClass3SingeltonCtor>(() =>
            {
                return new TestClass3SingeltonCtor("3");
            });


            di.AddTransient<TestClassTransient>();
            di.AddTransient<ITestClassTransient, TestClassTransient>(() =>
            {
                return new TestClassTransient();
            });
            di.AddTransient<ITestClassTransient2, TestClass2Transient>(() =>
            {
                return new TestClass2Transient("3");
            });

            DI.Initialize(di);


            // Singeltons
            {

                // Reference validation
                {

                    var testClass = DI.GetService<ITestClassSingelton>();
                    var testClass2 = DI.GetService<ITestClassSingelton>();
                    var testClass4 = DI.GetService<ITestClassSingelton>();

                    if (!((testClass == testClass2) &&
                        (testClass2 == testClass4)))
                    {
                        Debugger.Break();
                    }
                };

                {

                    var testClass = DI.GetService<TestClass2Singelton>();
                    var testClass2 = DI.GetService<TestClass2Singelton>();
                    var testClass4 = DI.GetService<TestClass2Singelton>();

                    if (!((testClass == testClass2) &&
                        (testClass2 == testClass4)))
                    {
                        Debugger.Break();
                    }
                };


                // Factory-initialization validation
                {
                    var testClass = DI.GetService<ITestClassSingelton>();

                    if (testClass.GUID != "1")
                    {
                        Debugger.Break();
                    }
                };

                {
                    var instance1 = DI.GetService<TestClassSingelton>();
                    var instace2 = DI.GetService<TestClassSingelton>();

                    if (instance1 != instace2)
                        Debugger.Break();

                }


                // Reference validation 2, w/ implementation factory
                {
                    var instance1 = DI.GetService<TestClassSingelton>();
                    var instace2 = DI.GetService<TestClassSingelton>();

                    if (instance1.GUID != instace2.GUID)
                        Debugger.Break();
                };

                {
                    var instance1 = DI.GetService<TestClassSingelton>();
                    var instace2 = DI.GetService<TestClassSingelton>();

                    if ((instance1.GUID != "2") ||
                        (instace2.GUID != "2"))
                        Debugger.Break();
                };


                // Reference validation 3, Non parameterless constructor w/ implementation factory 
                {
                    var instance = DI.GetService<TestClass3SingeltonCtor>();
                    var instance2 = DI.GetService<TestClass3SingeltonCtor>();

                    if (instance != instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<TestClass3SingeltonCtor>();
                    var instance2 = DI.GetService<TestClass3SingeltonCtor>();

                    if ((instance.GUID != "3") ||
                        (instance2.GUID != "3"))
                        Debugger.Break();
                };

            }


            // Transients
            {

                // Reference validation
                {

                    var instance = DI.GetService<TestClassTransient>();
                    var instance2 = DI.GetService<TestClassTransient>();

                    if (instance == instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<TestClassTransient>();
                    var instance2 = DI.GetService<TestClassTransient>();

                    if (instance.GUID == instance2.GUID)
                        Debugger.Break();
                };


                // Reference validation w/ interfaces
                {
                    var instance = DI.GetService<ITestClassTransient>();
                    var instance2 = DI.GetService<ITestClassTransient>();

                    if (instance == instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<ITestClassTransient>();
                    var instance2 = DI.GetService<ITestClassTransient>();

                    if (instance.GUID == instance2.GUID)
                        Debugger.Break();
                };

                // Reference validation, interfaces w/ ctor
                {
                    var instance = DI.GetService<ITestClassTransient2>();
                    var instance2 = DI.GetService<ITestClassTransient2>();

                    if (instance == instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<ITestClassTransient2>();
                    var instance2 = DI.GetService<ITestClassTransient2>();

                    if ((instance.GUID != "3") &&
                        (instance2.GUID != "3"))
                        Debugger.Break();
                };


            };

            Console.WriteLine("Hello World!");
        }
    }
}