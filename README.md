Quick Backup
============

Create backups, quick and easy.


## Usage

Run the interactive configuration generator with `./QuickBackup init` and follow
the instructions.

Keep in mind that creating a backup on the same storage device will not protect
from computer viruses or failure of the storage media. Use different devices
to store your backups, and keep them disconnected whenever possible.

```txt
./QuickBackup init
> Which directory would you like to create a backup for?
source [default: ./]: ~/Desktop/My Important Data/
> Where would you like to store your backups?
target [default: ~/Desktop/My Important Data/.backups]:
> How many boot-time backups would you like to keep?
boot [default: 0]: 2
> How many yearly backups would you like to keep?
yearly [default: 0]:
> How many monthly backups would you like to keep?
monthly [default: 5]:
> How many weekly backups would you like to keep?
weekly [default: 0]:
> How many daily backups would you like to keep?
daily [default: 5]:
> How many hourly backups would you like to keep?
hourly [default: 0]:
> Would you like to use hard links for subsequent backups instead of copying all files again (will save space)?
link [default: yes]:
> Would you like to just compare file size and last modification date to detect changes (will improve performance)?
fast [default: yes]:
> Where would you like to save your configuration?
[default: ~/Desktop/My Important Data/.backups/backup-config.json]:
Configuration file generated.
```

After configuring, run `./QuickBackup backup <path>` to generate a backup copy.

```
./QuickBackup backup "~/Desktop/My Important Data/"
Backing up 1 path(s)...
Backing up from '~/Desktop/My Important Data/' to '~/Desktop/My Important Data/.backups/2021-06-19_22-05-19_boot'...
Backup type: AtBoot, Daily
Latest backup path: '~/Desktop/My Important Data/.backups/2021-06-18_20-04-42'
'.' created
'Old Picture.jpg' unchanged, hard-linked
'Updated Document.docx' changed, copied
'.backups' skipped (excluded)
Done.
Cleaning old backups from '~/Desktop/My Important Data/.backups/.'...
Cleaning '~/Desktop/My Important Data/.backups/2021-06-13_15-04-53_boot'... ok
Done.
```

Ideally, you might want to configure your computer to launch this command
automatically at regular intervals, or whenever your backup media is inserted.

Run `./QuickBackup --help` to check all options and commands.

Run `./QuickBackup <command> --help` to check all options for a command.


## License

This project is licensed under the [MIT license](LICENSE).
