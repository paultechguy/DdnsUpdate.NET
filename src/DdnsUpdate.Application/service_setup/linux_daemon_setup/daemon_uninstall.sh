#!/bin/bash 

serviceName="ddnsupdate.service"

# ensure we're in app directory
if [[ ! -d ./linux_daemon_setup ]]; then
  echo "%Error: Run this script from the main app directory."
  exit 1
fi

warn_user_to_continue() {
    echo -e "\033[0;31mWarning: You are about to uninstall the \"$serviceName\" daemon.\033[0m"
    read -p "This action will remove application, configuration, and log files.  Do you want to continue (yes/no)? " response

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
if [[ -f /etc/systemd/system/$serviceName ]]; then
    # in case there is a network target, remove a symbolic link for it
    if [[ -f /etc/systemd/system/multi-user.target.wants/$serviceName ]]; then
        rm /etc/systemd/system/multi-user.target.wants/$serviceName
    fi

    systemctl stop $serviceName || true 2>/dev/null
    rm /etc/systemd/system/$serviceName
    systemctl daemon-reload

    # wait a bit before we begin removing folders
    echo 'Pausing while daemon is uninstalled...'
    sleep 3
else
    echo "%Error: The \"$serviceName\" is not installed."
    exit 1
fi

# main application
if [[ -f /usr/sbin/ddnsupdate ]]; then
  rm /usr/sbin/ddnsupdate
fi

# config dir and files
if [[ -d /etc/ddnsupdate ]]; then
  rm -fr /etc/ddnsupdate
fi

# log dir
if [[ -d /var/log/ddnsupdate ]]; then
  rm -fr /var/log/ddnsupdate
fi

# data dir
if [[ -d /var/lib/ddnsupdate ]]; then
  rm -fr /var/lib/ddnsupdate
fi

echo "The \"$serviceName\" daemon has been removed."