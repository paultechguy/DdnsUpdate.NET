#!/bin/bash 

serviceName="ddnsupdate.service"

# ensure we're in app directory
if [[ ! -d ./linux_daemon_setup ]]; then
  echo "%Error: Run this script from the main app directory."
  exit 1
else
  chmod 0755 ./linux_daemon_setup
  chmod 0755 ./linux_daemon_setup/*.sh
fi

warn_user_to_continue() {
    echo -e "\033[0;31mNotice: You are about to install the \"$serviceName\" daemon.\033[0m"
    read -p "This action will install application and configuration files.  Do you want to continue (yes/no)? " response

    if [[ $response != "yes" ]]; then
        return 1
    else
        return 0
    fi
}

if ! warn_user_to_continue; then
    exit 1
fi

# service file
if [[ ! -f /etc/systemd/system/$serviceName ]]; then
  cp ./linux_daemon_setup/$serviceName /etc/systemd/system/
  chmod 0755 /etc/systemd/system/$serviceName
  systemctl daemon-reload
  # redirect to dev/null to hide any msg about the creation of sym link
  systemctl enable $serviceName 2>/dev/null
else
    echo "%Error: The \"$serviceName\" is already installed."
    exit 1
fi

# main application
if [[ ! -f /usr/sbin/ddnsupdate ]]; then
  cp ddnsupdate /usr/sbin/
  chmod 0755 /usr/sbin/ddnsupdate
fi

# config dir and files
if [[ ! -d /etc/ddnsupdate ]]; then
  mkdir /etc/ddnsupdate
  chmod 0755 /etc/ddnsupdate
  cp ./config/appsettings*.json /etc/ddnsupdate/
fi

# log dir
if [[ ! -d /var/log/ddnsupdate ]]; then
  mkdir /var/log/ddnsupdate
  chmod 0755 /var/log/ddnsupdate
fi

# data dir
if [[ ! -d /var/lib/ddnsupdate ]]; then
  mkdir /var/lib/ddnsupdate
  chmod 0755 /var/lib/ddnsupdate
fi

# Done
echo "The \"$serviceName\" daemon is installed."
echo "You may now manage the daemon using the commands below:"
echo "   sudo systemctl start $serviceName"
echo "   sudo systemctl stop $serviceName"
echo "   sudo systemctl status $serviceName"
echo ""
echo "Note: Log files are stored in /var/log/ddnsupdate"
