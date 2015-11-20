import UIKit
import Socket_IO_Client_Swift

class ViewController: UIViewController, MSBClientManagerDelegate {
    
    //MARK: Properties
    
    @IBOutlet weak var labelStatusServer: UILabel!
    @IBOutlet weak var labelStatusBand: UILabel!
    @IBOutlet weak var labelStatusHeartRate: UILabel!
    @IBOutlet weak var labelStatusHeartRateSensor: UILabel!

    let socket = SocketIOClient(socketURL: "heartrate.azurewebsites.net");
    weak var client: MSBClient?

    
    override func viewDidLoad() {
        super.viewDidLoad()
        initSocket();
    }

    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
    }
    
    // Mark - Init
    func initSocket() {

        socket.on("connect") {data, ack in
            self.labelStatusServer.text = "Connected";
        }
        
        socket.on("haptic") {data, ack in
            // let color = data[0] as? String
            // self.labelStatusServer.text = color;
        }
        
        socket.connect()
    }
    

    // Mark - Heart rate helpers
    func getBandHeartRateConsent(completion: (result: Bool) -> Void) {
        let consent:MSBUserConsent? = self.client?.sensorManager.heartRateUserConsent()
        
        switch (consent) {
        case .Granted?:
            completion(result: true)
            break
        case .NotSpecified?, .Declined?:
            self.labelStatusHeartRateSensor.text =  "Getting access..."
            client?.sensorManager.requestHRUserConsentWithCompletion({ (consent:Bool, error:NSError!) -> Void in
                completion(result: consent)
            })
            break;
        default:
            completion(result: false)
            break;
        }
    }
    
    func reportHeartRate(heartRateData: MSBSensorHeartRateData!, error: NSError!) {
        self.labelStatusHeartRate.text = NSString(format: "%0.2d", heartRateData.heartRate) as String
        
        if (heartRateData.quality == MSBSensorHeartRateQuality.Locked) {
            self.labelStatusHeartRateSensor.text = "Locked"
            socket.emit("rate", heartRateData.heartRate)
            
        } else if (heartRateData.quality == MSBSensorHeartRateQuality.Acquiring)
        {
            self.labelStatusHeartRateSensor.text = "Acquiring"
        }
    }
    
    //MARK: UI Actions
    @IBAction func buttonStartRead(sender: UIButton) {
        MSBClientManager.sharedManager().delegate = self
        if let client = MSBClientManager.sharedManager().attachedClients().first as? MSBClient {
            self.client = client
            self.labelStatusBand.text = "Connecting...";
            MSBClientManager.sharedManager().connectClient(self.client)
        } else {
            self.labelStatusBand.text = "Can't connect";
        }
    }
    
    // Mark - MSBand Client Manager Delegates
    func clientManager(clientManager: MSBClientManager!, clientDidConnect client: MSBClient!) {
         self.labelStatusBand.text =  "Connected"
        
         self.getBandHeartRateConsent() {
            (result: Bool) in
            
            if (result) {
                self.labelStatusHeartRateSensor.text =  "Access granted"

                do {
                    try client.sensorManager.startHeartRateUpdatesToQueue(nil, withHandler: self.reportHeartRate)
                } catch let error as NSError {
                    print("Error: \(error.localizedDescription)")
                }
            } else {
                self.labelStatusHeartRateSensor.text =  "Access denied"
            }
        }
    }

    
    func clientManager(clientManager: MSBClientManager!, clientDidDisconnect client: MSBClient!) {
         self.labelStatusBand.text = "Disconnected"
    }
    
    func clientManager(clientManager: MSBClientManager!, client: MSBClient!, didFailToConnectWithError error: NSError!) {
        self.labelStatusBand.text = "Can't connect"
    }

}

