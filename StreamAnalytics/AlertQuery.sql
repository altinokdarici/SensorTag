Select ThermometerAmbTemp,MeasurementTime from DeviceInput
Group by TumblingWindow(Second, 20)
Having ThermometerAmbTemp > 30