using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        BreadMakingExample(3, 5, new[]{ 1, 2, 3, 4, 5 });
    }

    static void TrackRaceExample()
    {
        int numTracks = 5;
        int numRunners = 4;
        object[] tracks = new object[numTracks];
        double objectiveDistance = 5;
        MonitoredRunner[][] runners = new MonitoredRunner[numTracks][];

        //Create tracks
        for (int i = 0; i < numTracks; i++)
        {
            tracks[i] = new object();
        }

        // Create runners
        for (int i = 0; i < numTracks; i++)
        {
            runners[i] = new MonitoredRunner[numRunners];
            for (int j = 0; j < numRunners; j++)
            {
                runners[i][j] = new MonitoredRunner($"Runner {i + 1}.{j + 1}", objectiveDistance, tracks[i]);
            }
        }

        // Start the race
        for (int i = 0; i < numTracks; i++)
        {
            foreach (MonitoredRunner runner in runners[i])
            {
                Thread t = new Thread(new ThreadStart(runner.Run));
                t.Start();
            }
        }

        bool allFinished = false;
        // Wait for all runners to cross the finish line
        while (!allFinished)
        {
            allFinished = true;
            for (int i = 0; i < numTracks; i++)
            {
                foreach (MonitoredRunner runner in runners[i])
                {
                    if (!runner.hasFinished)
                    {
                        allFinished = false;
                        break;
                    }
                }
                if (!allFinished) { break; }
            }
        }
        Console.WriteLine("The race has ended!");
    }

    static void MultithreadExample()
    {
        int numRunners = 5;
        double objectiveDistance = 10;
        Runner[] runners = new Runner[numRunners];

        // Create runners
        for (int i = 0; i < numRunners; i++)
        {
            runners[i] = new Runner($"Runner {i + 1}", objectiveDistance);
        }

        // Start the race
        foreach (Runner runner in runners)
        {
            Thread t = new Thread(new ThreadStart(runner.Run));
            t.Start();
        }

        bool allFinished = false;
        // Wait for all runners to cross the finish line
        while (!allFinished)
        {
            allFinished = true;
            foreach (Runner runner in runners)
            {
                if (!runner.hasFinished)
                {
                    allFinished = false;
                    break;
                }

            }
        }
        Console.WriteLine("The race has ended!");
    }

    static void BreadMakingExample(int bakersTimeInSeconds, int bakersQuantityPerTime, int[] buyersQuantities)
    {
        CashRegister cashRegister = new CashRegister();
        Baker baker = new Baker(cashRegister, bakersQuantityPerTime, bakersTimeInSeconds);
        Buyer[] buyers = new Buyer[buyersQuantities.Length];

        //Create Buyers
        for (int i = 0; i < buyersQuantities.Length; i++)
        {
            buyers[i] = new Buyer(cashRegister, buyersQuantities[i], $"Buyer {i + 1}");
        }

        //Start Baking and Buying
        Thread baking = new Thread(baker.MakeBread);
        baking.Start();
        Thread[] buyingThreads = new Thread[buyers.Length];
        for (int i = 0; i < buyers.Length; i++)
        {
            buyingThreads[i] = new Thread(buyers[i].BuyBread);
            buyingThreads[i].Start();
        }
        foreach (Thread thread in buyingThreads)
        {
            if (thread.IsAlive)
            {
                thread.Join();
            }
        }
        baker.keepMaking = false;
    }
}

class Runner
{
    private string name;
    private double distanceCovered;
    private double objectiveDistance;
    private double speed;
    public bool hasFinished;

    public Runner(string name, double objectiveDistance)
    {
        this.name = name;
        this.objectiveDistance = objectiveDistance;
        this.distanceCovered = 0;
        this.speed = Math.Floor(new Random().NextDouble() * 100) / 100 + 0.1; // Random speed for each runner
        this.hasFinished = false;
    }

    public void Run()
    {
        Console.WriteLine($"{name} has started the race!");
        while (distanceCovered < objectiveDistance)
        {
            Thread.Sleep(100); // Simulate the time it takes to advance
            Advance();
        }
        Finish();
    }

    public void Advance()
    {
        distanceCovered += speed;
        Console.WriteLine($"{name} has advanced {speed} and covered {distanceCovered}.");
    }

    public void Finish()
    {
        this.hasFinished = true;
        Console.WriteLine($"-------------------------------\n{name} has crossed the finish line!\n-------------------------------");
    }
}

class MonitoredRunner
{
    private string name;
    private double distanceCovered;
    private double objectiveDistance;
    private double speed;
    private object track;
    public bool hasFinished;

    public MonitoredRunner(string name, double objectiveDistance, object track)
    {
        this.name = name;
        this.objectiveDistance = objectiveDistance;
        this.distanceCovered = 0;
        this.speed = Math.Floor(new Random().NextDouble() * 100) / 100 + 0.1; // Random speed for each runner
        this.hasFinished = false;
        this.track = track;
    }
    public void Run()
    {
        Monitor.Enter(track);
        try
        {
            Console.WriteLine($"-------------------------------\n{name} has started it's section!\n-------------------------------");
            while (distanceCovered < objectiveDistance)
            {
                Thread.Sleep(100); // Simulate the time it takes to advance
                Advance();
            }
            Finish();
        }
        finally
        {
            Monitor.Exit(track);
        }
    }
    public void Advance()
    {
        distanceCovered += speed;
        Console.WriteLine($"{name} has advanced {speed} and covered {distanceCovered}.");
    }

    public void Finish()
    {
        this.hasFinished = true;
        Console.WriteLine($"-------------------------------\n{name} has finished it's section!\n-------------------------------");
    }
}

class CashRegister
{
    private int BreadQuantity;
    public CashRegister()
    {
        BreadQuantity = 0;
    }
    public void SellBread(int Quantity)
    {
        Monitor.Enter(this);
        try
        {
            while (BreadQuantity < Quantity)
            {
                Console.WriteLine($"Stocked {BreadQuantity} bread(s) don't fulfill request, waiting for more");
                Monitor.Wait(this);
            }
            BreadQuantity -= Quantity;
            Console.WriteLine($"Selling {Quantity} bread(s). {BreadQuantity} bread(s) remaining");
            Monitor.Pulse(this); //Pulse in case the breads made fulfill next persons Quantity
        }
        finally { Monitor.Exit(this); }
    }

    public void StockBread(int Quantity)
    {
        Monitor.Enter(this);
        try
        {
            BreadQuantity += Quantity;
            Console.WriteLine($"Stocking {Quantity} bread(s) in Cash Register. Actual Quantity: {BreadQuantity}");
            Monitor.Pulse(this);
        }
        finally { Monitor.Exit(this); }
    }
}

class Baker
{
    private CashRegister cashRegister;
    private int breadPerTime;
    private int timeInMillis;
    public bool keepMaking;
    public Baker(CashRegister cashRegister, int breadPerTime, int timeInSeconds)
    {
        this.cashRegister = cashRegister;
        this.breadPerTime = breadPerTime;
        this.keepMaking = true;
        this.timeInMillis = timeInSeconds * 1000;
    }
    public void MakeMinuteBread()
    {
        Console.WriteLine("Started making Bread");
        Thread.Sleep(timeInMillis);
        Console.WriteLine("Finished making Bread, delivering Bread to Cash Register");
        cashRegister.StockBread(breadPerTime);
    }

    public void MakeBread()
    {
        while (keepMaking)
        {
            MakeMinuteBread();
        }
    }
}

class Buyer
{
    private CashRegister cashRegister;
    private int buyingQuantity;
    private string buyerName;
    public bool isFinished;

    public Buyer(CashRegister cashRegister, int buyingQuantity, string buyerName)
    {
        this.cashRegister = cashRegister;
        this.buyingQuantity = buyingQuantity;
        this.buyerName = buyerName;
        this.isFinished = false;
    }
    public void BuyBread()
    {
        Console.WriteLine($"{buyerName} is buying {buyingQuantity} bread(s)");
        this.cashRegister.SellBread(buyingQuantity);
        isFinished = true;
        Console.WriteLine($"{buyerName} has finished buying {buyingQuantity} bread(s)");
    }
}
