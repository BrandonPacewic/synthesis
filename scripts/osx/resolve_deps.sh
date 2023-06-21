#!/bin/sh

pretty_print() {
  printf "%b\n" "$1"
}

bad_print() {
  echo "\033[0;32m$1\033[0m"
}

good_print() {
  echo "\033[0;92m$1\033[0m"
}

prompt_yes_no() {
  local prompt_message="$1"
  local response

  while true; do
    read -rp "$prompt_message (Y/N): " response
    response=$(echo $response | tr '[:upper:]' '[:lower:]')

    if [[ $response == "y" ]]; then
      return 0  # Yes, return success
    elif [[ $response == "n" ]]; then
      return 1  # No, return failure
    else
      echo "Invalid input. Please enter either Y or N."
    fi
  done
}

check_internet_connection() {
  local host="google.com"
  local timeout=2

  if ping -q -w $timeout -c 1 $host >/dev/null 2>&1;then
    pretty_print "Connected to the internet"
  else
    bad_print "No internet connection - please connect to the internet to continue."
    exit
  fi
}

brew_install() {
    if ! command -v brew &> /dev/null
    then
        pretty_print "\nHomebrew is not installed - Install from https://brew.sh/ before continue"
        exit
    fi

    pretty_print "Updating Homebrew..."
    brew update


    local package="$1"
    local cask="${2:-true}";

    if prompt_yes_no "Install Brew Package $package ?"; then
        if [ "$cask" = "true" ];
        then
            brew install --cask $package
        else
            brew install $package
        fi;
    else
        bad_print "Please install necessary package and re-run"
    fi
}

dep_exists_brew() {
    local dep="$1"
    local package="$2"
    local cask="${3:-false}";

    if ! command -v $dep &> /dev/null
    then
        pretty_print "$dep could not be found - installing $package from homebrew"
        # Will exit and give error if there is no internet connection.
        check_internet_connection
        brew_install $package $cask
    else
        good_print "$dep is already installed!"
    fi
}

pretty_print "Installing necessary Synthesis Deps."

dep_exists_brew "protoc" "protobuf@23.3"
dep_exists_brew "dotnet" "dotnet" true

pretty_print "Finished Installing Dependencies"