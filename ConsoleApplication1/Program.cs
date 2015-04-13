using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Castle.DynamicProxy;
//using Castle.Core;


public class LoggingInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        Console.WriteLine("Log: Method Called: " + invocation.Method.Name);
        invocation.Proceed();
    }
}


namespace AspectOriented
{

    public class Avva
    {
        public virtual void A()
        {
            Console.WriteLine("A");
        }

        public virtual int B()
        {
            Console.WriteLine("B");
            return 100;
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hey");

            var generator = new ProxyGenerator();
            Avva myObject = generator.CreateClassProxy<Avva>(new LoggingInterceptor());

            myObject.A();
            int i = myObject.B();

            Console.WriteLine("I=" + i.ToString());


        }
    }
}
