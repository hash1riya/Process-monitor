#pragma warning disable CA1416
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ProcessMonitor.Utils;
public static class ProcessExtensions
{
	private static string TemporaryUserName => "<UPDATING>";
	private readonly static Dictionary<int, string> userNamesCache = [];
	public static string GetAssitiatedUserName(this Process process)
	{
		if (!userNamesCache.TryGetValue(process.Id, out var userName))
		{
			userNamesCache.Add(process.Id, TemporaryUserName);
			Task
				.Run(() => InternalGetUserName(Convert.ToUInt32(process.Id)))
				.ContinueWith(task => userNamesCache[process.Id] = task.Result);
		}
		return userNamesCache[process.Id];
	}

	private static string InternalGetUserName(uint processId)
	{
		IntPtr hProcess = IntPtr.Zero, hToken = IntPtr.Zero;
		try
		{
			hProcess = OpenProcess(AccessRights.PROCESS_QUERY_LIMITED_INFORMATION, false, Convert.ToUInt32(processId));
			if (hProcess == IntPtr.Zero)
				return "System";
			OpenProcessToken(hProcess, AccessRights.TOKEN_QUERY, out hToken);
			if (hToken == IntPtr.Zero)
				return "System";
			var identity = new WindowsIdentity(hToken);
			return identity.Name;
		}
		catch (Exception er)
		{
			Debug.WriteLine($"Error. GetUserNameForProcess.UpdateList: {er.Message}");
		}
		finally
		{
			if (hProcess != IntPtr.Zero)
				CloseHandle(hProcess);
			if (hToken != IntPtr.Zero)
				CloseHandle(hToken);
		}
		return string.Empty;
	}

	[DllImport("Advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseHandle(IntPtr hObject);
}

public static class AccessRights
{
	public static uint PROCESS_QUERY_LIMITED_INFORMATION => 0x1000;
	public static uint PROCESS_VM_READ => 0x0010;
	public static uint TOKEN_QUERY => 0x0008;
}