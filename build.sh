#!/bin/bash

# Configuration
ANDROID_PROJECT="RegistrationEasy.Android/RegistrationEasy.Android.csproj"
COMMON_PROJECT="RegistrationEasy.Common/RegistrationEasy.Common.csproj"
ANDROID_SDK_ROOT="D:\programfiles\Android\SDK"
APP_ID="com.TicketEasy.app"
KEYSTORE_PATH="RegistrationEasy.Android/registrationeasy.keystore"
KEY_ALIAS="80fafa"
KEY_PASS="80fafa"

# Convert Windows path to Unix path for Bash usage
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
    ANDROID_SDK_PATH=$(cygpath -u "$ANDROID_SDK_ROOT")
else
    ANDROID_SDK_PATH="$ANDROID_SDK_ROOT"
fi

EMULATOR_PATH="$ANDROID_SDK_PATH/emulator/emulator.exe"
ADB_PATH="$ANDROID_SDK_PATH/platform-tools/adb.exe"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

check_system_info() {
    echo -e "${BLUE}==========================================${NC}"
    echo -e "${BLUE}   System Environment Check               ${NC}"
    echo -e "${BLUE}==========================================${NC}"
    
    # 1. OS Version
    echo -ne "OS Version: \t"
    uname -srm

    # 2. .NET SDK Version
    echo -ne ".NET SDK: \t"
    if command -v dotnet &> /dev/null; then
        dotnet --version
    else
        echo -e "${RED}Not installed${NC}"
    fi

    # 3. Avalonia Version
    echo -ne "Avalonia Ver: \t"
    if [ -f "$COMMON_PROJECT" ]; then
        AVALONIA_VER=$(grep '<PackageReference Include="Avalonia"' "$COMMON_PROJECT" | grep -o 'Version="[^"]*"' | cut -d'"' -f2)
        if [ ! -z "$AVALONIA_VER" ]; then
            echo "$AVALONIA_VER"
        else
            echo -e "${RED}Unknown${NC}"
        fi
    else
        echo -e "${RED}Project file not found${NC}"
    fi

    # 4. Android SDK Info
    echo -ne "Android SDK: \t"
    if [ -d "$ANDROID_SDK_PATH" ]; then
        echo "$ANDROID_SDK_ROOT"
        # Try to find build-tools version
        if [ -d "$ANDROID_SDK_PATH/build-tools" ]; then
             LATEST_BUILD_TOOL=$(ls -1 "$ANDROID_SDK_PATH/build-tools" | sort -V | tail -n 1)
             echo -e "\t\t(Build Tools: $LATEST_BUILD_TOOL)"
        fi
    else
        echo -e "${RED}Not found at $ANDROID_SDK_ROOT${NC}"
    fi

    # 5. ADB Info
    echo -ne "ADB Status: \t"
    if [ -f "$ADB_PATH" ]; then
        echo -n "Found at $ADB_PATH - "
        "$ADB_PATH" version | head -n 1
    elif command -v adb &> /dev/null; then
        echo -n "Found in PATH - "
        adb version | head -n 1
    else
        echo -e "${RED}Not found in standard path${NC}"
    fi

    # 6. Emulator Info
    echo -ne "Emulator: \t"
    if [ -f "$EMULATOR_PATH" ]; then
        echo -n "Found at $EMULATOR_PATH"
        # Optional: check version if needed
        echo ""
    else
         echo -e "${RED}Not found${NC}"
    fi

    # 7. Java JDK Info
    echo -ne "Java JDK: \t"
    if command -v java &> /dev/null; then
        java -version 2>&1 | head -n 1
    else
        echo -e "${RED}Not found in PATH (Check JAVA_HOME)${NC}"
    fi

    # 8. HAXM/AEHD Check
    echo -ne "Acceleration: \t"
    if [ -f "$EMULATOR_PATH" ]; then
        ACCEL_CHECK=$("$EMULATOR_PATH" -accel-check 2>&1)
        if echo "$ACCEL_CHECK" | grep -q "is installed and usable"; then
            echo -e "Supported"
            echo -e "\t\t($ACCEL_CHECK)" | head -n 2 | tail -n 1
        else
            echo -e "${RED}Not supported or not installed${NC}"
            echo -e "\t\t$ACCEL_CHECK"
        fi
    else
        echo -e "${RED}Emulator not found${NC}"
    fi
    
    echo ""
}

ensure_keystore() {
    if [ ! -f "$KEYSTORE_PATH" ]; then
        echo -e "${BLUE}Generating Keystore at $KEYSTORE_PATH...${NC}"
        keytool -genkey -v -keystore "$KEYSTORE_PATH" -alias "$KEY_ALIAS" -keyalg RSA -keysize 2048 -validity 10000 \
            -storepass "$KEY_PASS" -keypass "$KEY_PASS" \
            -dname "CN=TicketEasy, OU=Development, O=80fafa, L=City, S=State, C=US"
        
        if [ $? -eq 0 ]; then
             echo -e "${GREEN}Keystore generated successfully.${NC}"
        else
             echo -e "${RED}Failed to generate keystore. Check if keytool is in PATH.${NC}"
             exit 1
        fi
    else
        echo -e "${GREEN}Keystore found at $KEYSTORE_PATH${NC}"
    fi
}

build_android() {
    echo -e "${GREEN}Building Android Application...${NC}"
    ensure_keystore
    
    # Build Common First
    echo "Building Common project..."
    dotnet build "$COMMON_PROJECT" -c Release
    if [ $? -ne 0 ]; then
        echo "Failed to build Common project!"
        exit 1
    fi

    dotnet build "$ANDROID_PROJECT" -c Release
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}Build Successful!${NC}"
        echo "APK location:"
        
        # Find Signed APK
        SIGNED_APK=$(find RegistrationEasy.Android/bin -name "*-Signed.apk" | head -n 1)
        if [ -z "$SIGNED_APK" ]; then
             # Try finding unsigned APK if signed one is not found
             SIGNED_APK=$(find RegistrationEasy.Android/bin -name "*.apk" | head -n 1)
        fi
        
        if [ -z "$SIGNED_APK" ]; then
             echo -e "${RED}APK not found in RegistrationEasy.Android/bin${NC}"
        else
             echo "$SIGNED_APK"
             
             # Check Architecture using aapt
             echo -e "${BLUE}Verifying Architecture:${NC}"
             
             # Find aapt (Android Asset Packaging Tool)
             if [ -d "$ANDROID_SDK_PATH/build-tools" ]; then
                 AAPT_PATH=$(ls -1 "$ANDROID_SDK_PATH/build-tools/"*"/aapt.exe" 2>/dev/null | sort -V | tail -n 1)
                 if [ -f "$AAPT_PATH" ]; then
                     # List native libraries in APK to confirm architecture
                     echo "Native libraries in APK:"
                     # Use 'unzip -l' as fallback or primary if aapt fails to list clearly
                     if command -v unzip &> /dev/null; then
                         unzip -l "$SIGNED_APK" | grep "lib/" | awk '{print $4}' | awk -F/ '{print $2}' | sort | uniq | sed 's/^/  - /'
                     else
                         # Fallback to aapt if unzip not available (though Git Bash usually has unzip)
                         "$AAPT_PATH" l -v "$SIGNED_APK" | grep "^lib/" | awk -F/ '{print $2}' | sort | uniq | sed 's/^/  - /'
                     fi
                 else
                     echo "aapt not found, cannot verify architecture."
                 fi
             fi
        fi
    else
        echo "Build Failed!"
    fi
}

run_android() {
    echo -e "${GREEN}Checking for Android Emulator...${NC}"
    
    # Check if emulator executable exists
    if [ ! -f "$EMULATOR_PATH" ]; then
        echo "Error: Emulator not found at $EMULATOR_PATH"
        exit 1
    fi

    # Get AVD
    AVD_NAME=$("$EMULATOR_PATH" -list-avds | head -n 1)
    
    if [ -z "$AVD_NAME" ]; then
        echo "No AVD found. Please create one in Android Device Manager."
        exit 1
    fi

    # Check if emulator is already running (via ADB)
    DEVICE_ID=$("$ADB_PATH" devices | grep "emulator-" | head -n 1 | awk '{print $1}')

    if [ -z "$DEVICE_ID" ]; then
        echo -e "${GREEN}Launching Emulator: $AVD_NAME ...${NC}"
        # Launch emulator in background (Removed -gpu host as it caused black screen)
        "$EMULATOR_PATH" -avd "$AVD_NAME" &
        
        echo "Waiting for emulator to become ready..."
        "$ADB_PATH" wait-for-device
        
        # Double check device ID
        DEVICE_ID=$("$ADB_PATH" devices | grep "emulator-" | head -n 1 | awk '{print $1}')
    else
        echo -e "${GREEN}Emulator already running: $DEVICE_ID${NC}"
    fi
    
    echo "Waiting for boot completion..."
    # Wait until boot is completed
    while [ "$("$ADB_PATH" -s "$DEVICE_ID" shell getprop sys.boot_completed | tr -d '\r')" != "1" ]; do
        sleep 1
    done
    
    echo "Waiting for emulator to stabilize (10s)..."
    sleep 10

    echo -e "${GREEN}Building Android App...${NC}"
    ensure_keystore
    
    # Build Common First
    dotnet build "$COMMON_PROJECT" -c Debug
    if [ $? -ne 0 ]; then echo "Common Build Failed"; exit 1; fi

    # Build Android
    dotnet build "$ANDROID_PROJECT" -c Debug
    if [ $? -ne 0 ]; then echo "Android Build Failed"; exit 1; fi

    # Find APK (Debug)
    SIGNED_APK=$(find RegistrationEasy.Android/bin/Debug -name "*-Signed.apk" | head -n 1)
    if [ -z "$SIGNED_APK" ]; then
         SIGNED_APK=$(find RegistrationEasy.Android/bin/Debug -name "*.apk" | head -n 1)
    fi

    if [ -z "$SIGNED_APK" ]; then
        echo -e "${RED}APK not found!${NC}"
        exit 1
    fi

    echo -e "${GREEN}Deploying APK: $SIGNED_APK${NC}"
    
    # Uninstall old app
    echo "Uninstalling old version..."
    "$ADB_PATH" -s "$DEVICE_ID" uninstall "$APP_ID" > /dev/null 2>&1

    # Install new app
    echo "Installing new version..."
    "$ADB_PATH" -s "$DEVICE_ID" install -r "$SIGNED_APK"

    # Clear Logcat
    "$ADB_PATH" -s "$DEVICE_ID" logcat -c

    # Launch App (Explicit Activity)
    echo -e "${GREEN}Launching App...${NC}"
    "$ADB_PATH" -s "$DEVICE_ID" shell am start -n "$APP_ID/$APP_ID.MainActivity"
    
    echo -e "${BLUE}Monitoring Logcat for Errors (Press Enter to stop monitoring without killing emulator)...${NC}"
    # Start logcat in background
    "$ADB_PATH" -s "$DEVICE_ID" logcat -v time "*:E" "$APP_ID:D" | grep -E "$APP_ID|FATAL|RuntimeError" &
    LOGCAT_PID=$!
    
    # Wait for user input to stop monitoring
    read -p ""
    
    # Kill logcat process but leave emulator running
    kill $LOGCAT_PID 2>/dev/null
    echo -e "${GREEN}Logcat monitoring stopped. Emulator is still running.${NC}"
}

clean_android() {
    echo -e "${GREEN}Cleaning Android App...${NC}"
    
    # Check ADB
    if [ ! -f "$ADB_PATH" ]; then
        echo -e "${RED}ADB not found at $ADB_PATH${NC}"
        return
    fi
    
    # Check if emulator/device is connected
    DEVICE_ID=$("$ADB_PATH" devices | grep "emulator-" | head -n 1 | awk '{print $1}')
    if [ -z "$DEVICE_ID" ]; then
         # Try to find a real device
         DEVICE_ID=$("$ADB_PATH" devices | grep -v "List of devices attached" | grep "device" | head -n 1 | awk '{print $1}')
    fi
    
    if [ -z "$DEVICE_ID" ]; then
        echo "No device/emulator connected."
        return
    fi
    
    # Uninstall
    echo "Uninstalling $APP_ID..."
    "$ADB_PATH" -s "$DEVICE_ID" uninstall "$APP_ID"
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}Uninstalled successfully.${NC}"
    else
        echo -e "${RED}Uninstall failed (App might not be installed).${NC}"
    fi
}

fix_emulator() {
    echo -e "${GREEN}Fixing Emulator (Cold Boot & Wipe Data)...${NC}"
    
    # Check if emulator executable exists
    if [ ! -f "$EMULATOR_PATH" ]; then
        echo "Error: Emulator not found at $EMULATOR_PATH"
        exit 1
    fi

    # Get AVD
    AVD_NAME=$("$EMULATOR_PATH" -list-avds | head -n 1)
    
    if [ -z "$AVD_NAME" ]; then
        echo "No AVD found."
        exit 1
    fi

    echo "Target AVD: $AVD_NAME"
    
    # Check if emulator is running and kill it
    DEVICE_ID=$("$ADB_PATH" devices | grep "emulator-" | head -n 1 | awk '{print $1}')
    if [ ! -z "$DEVICE_ID" ]; then
        echo "Stopping running emulator: $DEVICE_ID..."
        "$ADB_PATH" -s "$DEVICE_ID" emu kill
        sleep 5
    fi
    
    # Force kill any remaining emulator processes (Windows)
    echo "Force killing any hung emulator processes..."
    taskkill //F //IM emulator.exe //T 2>/dev/null
    taskkill //F //IM qemu-system-x86_64.exe //T 2>/dev/null
    sleep 2

    echo -e "${BLUE}Cleaning up emulator lock files...${NC}"
    
    # AVDs are usually in $HOME/.android/avd/
    # Try to find the AVD directory
    AVD_DIR=$(find "$HOME/.android/avd" -name "${AVD_NAME}.avd" | head -n 1)
    if [ ! -z "$AVD_DIR" ]; then
         echo "Found AVD dir: $AVD_DIR"
         rm -f "$AVD_DIR"/*.lock
         echo "Removed lock files."
    else
         echo "Could not find AVD directory automatically."
    fi
    
    echo -e "${GREEN}Emulator stopped and lock files cleaned.${NC}"
    echo -e "${BLUE}Note: To fully wipe data, you can run: ${NC}"
    echo -e "${BLUE}$EMULATOR_PATH -avd $AVD_NAME -wipe-data${NC}"
}

# Run the check
check_system_info

# Handle Arguments
if [ -n "$1" ]; then
    choice="$1"
else
    echo -e "${BLUE}==========================================${NC}"
    echo -e "${BLUE}   TicketEasy Build Manager         ${NC}"
    echo -e "${BLUE}==========================================${NC}"
    echo "Please select an option:"
    echo "1) Build Android APK"
    echo "2) Launch Android Emulator & Run App"
    echo "3) Stop & Uninstall Android App"
    echo "4) Fix Emulator (Cold Boot & Wipe Data)"
    echo "q) Quit"
    read -p "Enter choice [1-4]: " choice
fi

case $choice in
    1) build_android ;;
    2) run_android ;;
    3) clean_android ;;
    4) fix_emulator ;;
    q) echo "Exiting."; exit 0 ;;
    *) echo "Invalid option."; exit 1 ;;
esac
