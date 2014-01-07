using System;
using System.Dynamic;
using System.Reactive.Linq;

namespace FirstClassEventGUIArchitecture
{
    //Numeric up-down control
#region Functional

    using IView = IObservable<Events>;

    enum Events { Incr, Decr };
    
    class Model
    {
        public Model(int state) { this.State = state; }
        public int State { get; private set; }
    }

    delegate Model Controller(Model model, Events @event);

    delegate Model Mvc(Controller controller, Model model, IView view);

#endregion

    static class Program
    {
        static void Main()
        {
            Action<dynamic, Events> controller = (model, @event) =>
            {
                switch (@event)
                {
                    case Events.Incr:
                        model.State++;
                        break;
                    case Events.Decr:
                        model.State--;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                Console.WriteLine("State: {0}", model.State);
            };

            dynamic seed = new ExpandoObject();
            seed.State = 6;

            Func<Action<dynamic, Events>, ExpandoObject, IObservable<Events>, IDisposable> mvc =
                (ctrl, m, view) => view.Subscribe(@event => ctrl(seed, @event));

            var viewInstance = new[] { Events.Incr, Events.Decr, Events.Incr }.ToObservable();
            var eventLoop = mvc(controller, seed, viewInstance);
            Console.WriteLine("Press <ENTER> to stop");
            Console.ReadLine();
        }
    }
}
