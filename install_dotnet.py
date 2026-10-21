import urllib.request
import zipfile
import os
import sys
import shutil

url = "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.402/dotnet-sdk-10.0.402-win-x64.zip"
target_dir = os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\dotnet")
zip_path = os.path.expandvars(r"%TEMP%\dotnet-sdk-10.zip")

print(f"Target directory: {target_dir}")
print(f"Downloading {url} ...")

os.makedirs(target_dir, exist_ok=True)

class DownloadProgressBar:
    def __init__(self):
        self.last_percent = -1
    def __call__(self, block_num, block_size, total_size):
        downloaded = block_num * block_size
        if total_size > 0:
            percent = int(downloaded * 100 / total_size)
            if percent != self.last_percent and percent % 10 == 0:
                print(f"Downloaded {percent}% ({downloaded // (1024*1024)} MB / {total_size // (1024*1024)} MB)")
                self.last_percent = percent

try:
    urllib.request.urlretrieve(url, zip_path, DownloadProgressBar())
    print(f"Download complete: {os.path.getsize(zip_path)} bytes")
    
    print("Extracting ZIP archive...")
    with zipfile.ZipFile(zip_path, 'r') as zip_ref:
        zip_ref.extractall(target_dir)
    print("Extraction complete!")
    
    dotnet_exe = os.path.join(target_dir, "dotnet.exe")
    if os.path.exists(dotnet_exe):
        print(f"Verified: {dotnet_exe} exists!")
    else:
        print("Warning: dotnet.exe not found in target dir")
        
    os.remove(zip_path)
    print("Cleanup complete. .NET SDK 10 is ready!")
except Exception as e:
    print(f"Error: {e}")
    sys.exit(1)
