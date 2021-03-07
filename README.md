# Cleipnir Persistent Programming in .NET

Cleipnir is a .NET (still experimental) framework allowing persistent programming on the .NET platform.

## Background
As part of my (now failed) PhD I investigated how persistent programming could simplify the implementation of distributed systems and in particular consensus algorithms such as Paxos and Raft. I think there is a potential for persistent programming that has still not been reached in industry. Especially, for the Saga microservice pattern. Hang in there and see if you agree :)

## What is Persistent Programming?
Persistent programming is a programming paradigm where the process state is persisted automatically by the runtime at well-coordinated points in time. Furthermore, the runtime will automatically reinstate the state of the process when it is restarted. Crucially, this occurs without explicitly instructed so in application code.

### Ping-Pong Example
To clarify, consider the toy example of Pinger and Ponger, where each performs the following:

#### Pinger
1. Waits a second
2. Sends a message “Ping” to Ponger
3. Waits for a reply from Ponger afterwards restart form step 1.
#### Ponger 
1. Waits for a “Ping” from Pinger
2. Send a “Pong” to Pinger restart from step 1.

The functionality can be implemented using two classes and a main method. 
```csharp
class Pinger : IPropertyPersistable
{
  public Source<string> Messages { get; set; }
  public int Count { get; set; }

  public async CTask Start()
  {
    while (true)
    {
      await Sleep.Until(1000);
      Messages.Emit($"PING {Count++}");
      var msg = await Messages.Where(m => m.StartsWith("PONG")).Next();
      Console.WriteLine("PINGER: " + msg);
    }
  }
}
```
```csharp
class Ponger : IPropertyPersistable
{
  public Source<string> Messages { get; set; }
  public int Count { get; set; }

  public async CTask Start()
  {
    while (true)
    {
      var msg = await Messages.Where(s => s.StartsWith("PING")).Next();
      Console.WriteLine($"PONGER: {msg}" );
      await Sleep.Until(1000);
      Messages.Emit($"PONG {Count++}");
    }
  }
}
```
```csharp
public static void Main()
{
  var storage = new SimpleFileStorageEngine(@".\PingPong.txt", deleteExisting: true);
  //you can also use new SqlServerStorageEngine(...) and new InMemoryStorageEngine()
  var engine = ExecutionEngineFactory.StartNew(storage);
  engine.Schedule(() =>
  {
     var messages = new Source<string>();
     var pinger = new Pinger { Messages = messages };
     var ponger = new Ponger { Messages = messages };
     
     _ = ponger.Start();
     _ = pinger.Start();
   });

  while (true)
  {
    Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
    Console.ReadLine();

    engine.Dispose();

    Console.WriteLine("PRESS ENTER TO START PING PONG APP");
    Console.ReadLine();
     
    engine = ExecutionEngineFactory.Continue(storage);
   }
}
```

When executing the toy example the process state is persisted when hitting enter the first time and subsequently loaded from persistent storage when hitting enter the second time. Thus, after the process state has persisted the first time. You can continue the process execution across restarts by changing the Main-method as follows:

```csharp
public static void Main()
{
  var storage = new SimpleFileStorageEngine(@".\PingPong.txt", deleteExisting: false);
  var scheduler = ExecutionEngineFactory.Continue(storage);
          
  while (true)
  {
    Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
    Console.ReadLine();

    scheduler.Dispose();

    Console.WriteLine("PRESS ENTER TO START PING PONG APP");
    Console.ReadLine();

    scheduler = ExecutionEngineFactory.Continue(storage);
  }
}
```

Somewhat simplified, we can see from the example that persistent programming is the ability to store the complete state of the executing process automatically by the runtime. By the complete state we mean both the object graph (Pinger, Ponger and Source) and the state of “threads” (sleeping in Pinger or Ponger).

## Sagas
Before explaining how Cleipnir can be used to realize the Saga microservice pattern let us quickly review the pattern. The goto example in my mind is the travel agent who needs to complete the booking of a holiday including both flight and hotel for a customer. The travel agent’s workflow also entails reserving and deducting funds from the customer's credit card. Conceptually, the complete workflow is simple and can be summarized as follows:
1. Reserve amount on customer’s credit card
2. If it fails abort the workflow
3. Otherwise, book hotel
4. If it fails cancel the credit card reservation and abort
5. Otherwise, book flight
6. If it fails cancel the credit card reservation and hotel booking then abort
7. Finally, deduct amount from credit card and cancel reservation 

As the services required by the workflow are provided by three different companies using an ACID transaction is not an option. Thus, we (the developer) must ensure that any already performed actions are reversed (so called compensating action) if an error occurs at some point in the workflow. The issue is complicated by the fact that the reversed action must be performed despite process crashes. As such the workflow cannot be implemented as an ordinary method without using some orchestration framework or other persistence mechanism. Either way, my limited experience is that the end result is not that simple nor convenient for the developer. 

Using persistent programming on the other hand changes the situation. In Cleipnir the workflow can be implemented as follows:

```csharp
public async CTask Do()
{
  var compensatingActions = new CList<Func<CTask>>();

  try
  {
    var reservationId = await BankConnection.ReserveFunds(1_000, "1234-1234-1234-1234");
    compensatingActions.Add(() => BankConnection.CancelReservation(reservationId));
    var carBookingId = await AirlineConnection.Book("Mazda 2 Sport");
    compensatingActions.Add(() => AirlineConnection.CancelBooking(carBookingId));
    var hotelBookingId = await HotelConnection.Book("Hotel Dangleterre Suite");
    compensatingActions.Add(() => HotelConnection.CancelBooking(hotelBookingId));
    await BankConnection.DeductFunds(reservationId);
  }
  catch (Exception e)
  {
    compensatingActions.ForEach(a => a());
    throw;
  }

  Console.WriteLine("TRAVEL AGENT: Workflow completed!");
}
```

The source code is pretty much a straightforward implementation of the workflow in C#. If the process fails during the execution of the method Cleipnir ensures that the execution will continue when the process is brought back up again. The example source code is available in our repository if you want to play around with Cleipnir and test the different failure scenarios. 

## What is going on?
I know from myself that when presented with a software abstraction that I would immediately try to pick it apart and understand how it works underneath its shiny surface. Hopefully, the elevator speech above has sparked your interest enough to hang in there for the nitty gritty details. 

Persistent programming in Cleipnir is realized using:
Object Store
Capable of persisting and retrieving object graphs from a storage engine.
Storage Engine
Persists and loads object entries to and from persistent storage. Currently, three implementations exist: SQL Server, Single file log and an in memory implementation for testing purposes.
Execution Engine
Responsible for executing the Cleipnir application and coordinating when to persist the process state and start the process after a restart. 

### Object Store:
Cleipnir’s object store is a rudimentary object database capable of persisting and fetching object graphs from a storage engine. The benefit of the object store is that it is able to persist delegates and Cleipnir’s custom tasks.

For instance, consider the following example of persisting an action:

```csharp
public static void Main() 
{
  var storageEngine = new InMemoryStorageEngine();
  var os = new ObjectStore(storageEngine);
  var sayer = new Sayer();
  sayer.Greeting = "Hello";
  Action greet = sayer.Greet;
            
  os.Attach(sayer);
  os.Attach(greet);
            
  os.Persist();

  os = ObjectStore.Load(storageEngine);

  greet = os.Resolve<Action>();
  greet(); // Hello
  os.Resolve<Sayer>().Greeting = "G'day";
  greet(); // G’day
}

public class Sayer : IPropertyPersistable
{
  public string Greeting { get; set; }

  public void Greet() => Console.WriteLine(Greeting);
}
```

Being able to persist delegate instances is not something new in .NET. Essentially, a delegate is just a method pointer (instance or static). However, it is somewhat frowned upon as the delegate may point to arbitrary code and possibly compiler generated classes. In Cleipnir we jump straight into the deep end and knowingly take the risk. Cleipnir’s object store is capable of persisting compiler generated classes such as display classes and state machines. E.g.

```csharp
os.Attach((Action) (() => Console.WriteLine("Come join us!")));
```
