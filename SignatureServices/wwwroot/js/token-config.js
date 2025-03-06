const driverMap = {
    'ePass2003': 'eps2003csp11.dll',
    'WD_PROXKEY': 'w_pkcs11.dll',
    'Egypt Trust': 'eps2003csp11.dll',
    'SoftHSM2': 'softhsm2-x64.dll'
};

document.getElementById('usbToken').addEventListener('change', function() {
    const tokenType = this.value;
    const driverName = driverMap[tokenType] || 'Please select a token';
    document.getElementById('expectedDriver').textContent = driverName;
});

function togglePinVisibility() {
    const pinInput = document.getElementById('pin');
    const toggleButton = event.currentTarget;
    if (pinInput.type === 'password') {
        pinInput.type = 'text';
        toggleButton.innerHTML = '<i class="bi bi-eye-slash"></i>';
    } else {
        pinInput.type = 'password';
        toggleButton.innerHTML = '<i class="bi bi-eye"></i>';
    }
}

function testConnection() {
    const form = document.getElementById('tokenForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const tokenType = document.getElementById('usbToken').value;
    const pin = document.getElementById('pin').value;
    
    const request = {
        LibraryPath: driverMap[tokenType],
        Pin: pin,
        Data: "Hello, World!"
    };

    fetch('/api/DigitalSignature/SignDocument', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
    })
    .then(response => {
        console.log('Raw Response:', response);
        return response.json();
    })
    .then(signData => {
        console.log('API Response Data:', signData);
        console.log('Token Info:', signData.tokenInfo);
        
        if (!signData.success) {
            throw new Error(signData.message || 'Failed to sign data');
        }
        
        showCertificateInfo({
            status: "Success",
            tokenInfo: signData.tokenInfo,
            signature: signData.signature
        });
        
        document.getElementById('saveButton').disabled = false;
        document.getElementById('saveSettings').disabled = false;
    })
    .catch(error => {
        console.error('Error:', error);
        showCertificateInfo({
            status: "Error",
            error: error.message
        });
    });
}

function showCertificateInfo(info) {
    const certDetails = document.getElementById('certDetails');
    certDetails.value = `Token Information:
Label: ${info.tokenInfo.label}
Model: ${info.tokenInfo.model}
Serial Number: ${info.tokenInfo.serialNumber}

Certificate Information:
Subject: ${info.tokenInfo.certificateSubject}
Issuer: ${info.tokenInfo.certificateIssuer}
Valid Until: ${info.tokenInfo.validUntil}

Signature (Base64):
${info.signature}`;
    document.getElementById('certInfo').classList.remove('d-none');
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    document.getElementById('usbToken').dispatchEvent(new Event('change'));
});