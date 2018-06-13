using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Timers;

public class MulPing
{
	static int PingCount=-1;
	static ConcurrentBag<IPStat> statuses; 
	static int CurPos=4;
	static void Main(string[] args)
	{ 
		System.Timers.Timer timer = new System.Timers.Timer(100); //timer updating progress percentage
	    	timer.Elapsed += async ( sender, e ) => await HandleTimer();
	   	IP FirstIP = null, LastIP = null; 
		int Timeout = 0;
		
		//checking if any arguments were passed
		if (args != null) 
		{
			IP.TryParse((from a in args where a.Substring(0,2)=="-f" select a.Substring(2)).FirstOrDefault(),out FirstIP);
			IP.TryParse((from a in args where a.Substring(0,2)=="-l" select a.Substring(2)).FirstOrDefault(),out LastIP);
			Int32.TryParse((from a in args where a.Substring(0,2)=="-t" select a.Substring(2)).FirstOrDefault(),out Timeout);
		}
		while (PingCount<0) //can continue only when FirstIP is lower than LastIP
		{
			while (FirstIP==null)
			{
				Console.WriteLine("First IP Address: ");
				IP.TryParse(Console.ReadLine(),out FirstIP);
				if (FirstIP==null) Console.WriteLine("Enter correct IP address");	
			}
	
			while (LastIP==null)
			{
				Console.WriteLine("Last IP address: ");
				IP.TryParse(Console.ReadLine(),out LastIP);
				if (LastIP==null) Console.WriteLine("Enter correct IP address");
			}
		
			PingCount = LastIP-FirstIP;
			if (PingCount<0) 
			{
				Console.WriteLine("First IP address must be lower than last");
				FirstIP=null;
				LastIP=null;
			}
		}
		
		while (Timeout<=0) //can continue only if Timeout is higher than 0
		{
			Console.WriteLine("Ping timeout [ms]: ");
			Int32.TryParse(Console.ReadLine(),out Timeout);
			if (Timeout<=0) Console.WriteLine("Enter positive integer value");		
		}
	
		statuses = new ConcurrentBag<IPStat>(); 
		CurPos=Console.CursorTop;
		timer.Start();
		
		IP[] addresses = IP.GetArray(FirstIP,LastIP); //creating array with IPs to ping
		
		//pinging IPs
		Parallel.ForEach (addresses, ip => 
		{
			Ping pingSender = new Ping();
			PingReply reply = pingSender.Send(ip, Timeout);

			if (reply.Status == IPStatus.Success)
            		{
				IPStat ipStat;
				try //if reply is recieved, trying to get HostName
				{
					IPHostEntry hostEntry = Dns.GetHostEntry(ip);
					ipStat=new IPStat(ip,true,reply.RoundtripTime.ToString(),hostEntry.HostName.ToString());
				}
				catch
				{
					ipStat=new IPStat(ip,true,reply.RoundtripTime.ToString());
				}
                		statuses.Add(ipStat);
            		}
            		else
            		{
                		statuses.Add(new IPStat(ip,false));
            		}
     
		});
	
		var statusesSorted = from s in statuses orderby s.ip select s; //sorting IPStats by IP
	
		Console.WriteLine();
		timer.Stop();
		timer.Dispose();
		Console.SetCursorPosition(0, CurPos);
		Console.WriteLine("    ");
		Console.WriteLine(String.Format("{0,-18}{1,-18}{2,-10}{3,-60}","IP address","Reply","Time","Name")); //table header
		
		foreach (var s in statusesSorted) //building table
			Console.WriteLine(s.ShowStat()); 

		Console.ReadLine();
	}


	private static Task HandleTimer() //updating progress percentage once in 100 ms
	{
		return Task.Run(()=>{
			Console.SetCursorPosition(0, CurPos);
			Console.Write("{0:P0}",(double)statuses.Count/PingCount);		
		});
	}

}	

public class IP :IPAddress,IComparable,IComparable<IP>	//enhanced IPAddress class
	{
	public string IPString {get; private set;}
	public uint IPInt {get; private set;}
	
	public IP(byte[] address):base(address)
	{
		IPString=ToString();
		byte[] buffer = new byte[4];
		byte[] bytes = GetAddressBytes();
		buffer[3]=bytes[0];
		buffer[2]=bytes[1];
		buffer[1]=bytes[2];
		buffer[0]=bytes[3];
		IPInt=BitConverter.ToUInt32(buffer,0);
	}
	
	public override bool Equals(object otherIP)
	{	
		if (!(otherIP is IP)) 
			return false;
		return (IPInt==((IP)otherIP).IPInt);
	}
		
	public override int GetHashCode() {return (int)IPInt;}
	
	public int CompareTo (IP otherIP) 
	{
		if (Equals (otherIP)) return 0;
		return IPInt.CompareTo(otherIP.IPInt);
	}
	
	int IComparable.CompareTo (object otherObject)
	{
		if (!(otherObject is IP))
		throw new InvalidOperationException ("CompareTo: it is not IP object");
		return CompareTo ((IP)otherObject);
	}	
	
	
	
	public static bool operator < (IP ip1, IP ip2)
	{return ip1.CompareTo(ip2)==-1;}
	
	public static bool operator == (IP ip1, IP ip2)
	{ return Equals(ip1,ip2);}
		
	public static bool operator != (IP ip1, IP ip2)
	{return !Equals(ip1,ip2);}
	
	public static bool operator <= (IP ip1, IP ip2)
	{return ip1.CompareTo(ip2)==-1 || ip1.CompareTo(ip2)==0;}
		
	public static bool operator >= (IP ip1, IP ip2)
	{return ip1.CompareTo(ip2)==1 || ip1.CompareTo(ip2)==0;}
		
	public static bool operator > (IP ip1, IP ip2)
	{return ip1.CompareTo(ip2)==1;}
		
	public static int operator - (IP ip1, IP ip2)
	{
		if ((int)(ip1.IPInt-ip2.IPInt)>=0) return (int)(ip1.IPInt-ip2.IPInt+1);
		else return (int)(ip1.IPInt-ip2.IPInt)-1;
	}	
		
	public static IP operator + (IP ip1, int adding)
	{
		byte[] bytes = BitConverter.GetBytes(ip1.IPInt+adding);
		byte[] buffer = new byte[4];
		buffer[3]=bytes[0];
		buffer[2]=bytes[1];
		buffer[1]=bytes[2];
		buffer[0]=bytes[3];
		return new IP(buffer);
	}	
		
	public static new IP Parse(string s)
	{
		return new IP(IPAddress.Parse(s).GetAddressBytes());
	}
		
	public static bool TryParse(string s, out IP address)
	{
		IPAddress IPA;
		bool ok = IPAddress.TryParse(s,out IPA) && ((from c in s where c=='.' select c).Count() == 3);
		if (ok) address = new IP(IPAddress.Parse(s).GetAddressBytes());
		else address = null;
		return ok;
	}	
		
	public static IP[] GetArray(IP ipFirst, IP ipLast)
	{
		IP[] IPArray = new IP[ipLast-ipFirst];
		for (int i=0;i<ipLast-ipFirst;i++)
			IPArray[i]=ipFirst+i;
	
		return IPArray;
	}
}
	
public class IPStat
{
	public IP ip{get; private set;}
	bool reply;
	string time;
	string name;
	
	public IPStat(IP ip, bool reply, string time=null, string name=null)
	{
		this.ip=ip;
		this.reply=reply;
		this.time=time;
		this.name=name;
	}

	public string ShowStat()
	{
		return String.Format("{0,-18}{1,-18}{2,-10}{3,-60}",ip,reply?"Reply recieved":"No reply",time==null?"":time+" ms",name??"");
	}
		
}
	