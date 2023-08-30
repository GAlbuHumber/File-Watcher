# File-Watcher
The Scope of this project is to provide a backup for files transmitted over udp.
# Setup
The program needs initial setup to register the process to a service
sc create FileWatcherService binPath= "C:\path\to\your\project\bin\Debug\net6.0\FileWatcher.exe"
sc start FileWatcherService
Then its safe to start from visual studio/local debug
It should now create the destination folder with the title backup_date
It creates this anytime once daily after 12.01pm



