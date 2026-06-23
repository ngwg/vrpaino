#import <AVFoundation/AVFoundation.h>

extern "C" {

    // Returns: 0 = not determined, 1 = granted, 2 = denied/restricted
    int _CameraPermissionStatus() {
        AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
        if (status == AVAuthorizationStatusAuthorized) return 1;
        if (status == AVAuthorizationStatusNotDetermined) return 0;
        return 2;
    }

    // Requests camera permission. Result is retrieved by polling _CameraPermissionStatus.
    void _RequestCameraPermission() {
        [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
            // Result will be picked up by _CameraPermissionStatus on next call
        }];
    }
}
