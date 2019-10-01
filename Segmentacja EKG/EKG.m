close all;
clear all;
clc;
sig = load('sygnaly.txt');
ECG_data_unfiltered = sig(:,2);

plot(ECG_data_unfiltered)
title('Orginalny Sygnal EKG')



ECG_data = sig(:,3);
figure
hold on
grid on
ax = axis; axis([0 1850 -2.2 2.2])
plot(ECG_data)
title('Filtrowany Sygnal EKG')

figure
hold on
plot(ECG_data);
axis([0 1850 -2.2 2.2]); grid on;
xlabel('Czas / milisekundy'); ylabel('Napiecie (mV)')
title('Posegmentowany sygnal EKG')

[~,locs_Rwave] = findpeaks(ECG_data,'MinPeakHeight',0.5,'MinPeakDistance',120);

ECG_inverted = -ECG_data;
[~,locs_SQwave] = findpeaks(ECG_inverted,'MinPeakHeight',0.09, 'MinPeakDistance', 25); 

[~,locs_Twave] = findpeaks(ECG_data,'MinPeakHeight',-0.1,'MinPeakDistance',70);
% [~,locs_qwave] = findpeaks(ECG_inverted,'Threshold',1e-4);
locs_Qwave = locs_SQwave(ECG_data(locs_SQwave)>-0.13 & ECG_data(locs_SQwave)<0);

locs_Swave = locs_SQwave(ECG_data(locs_SQwave)<-0.13);


 plot(locs_Rwave,ECG_data(locs_Rwave),'rv','MarkerFaceColor','r');
 plot(locs_Swave,ECG_data(locs_Swave),'rs','MarkerFaceColor','b');
 plot(locs_Twave,ECG_data(locs_Twave),'X','MarkerFaceColor','g'); 
 plot(locs_Qwave,ECG_data(locs_Qwave),'x','MarkerFaceColor','y');

legend('Sygnal EKG','Fale R','Fale S ', 'Fale Q', 'Fale P i T');


beat_count = length(locs_Rwave);
disp(beat_count);
fs = 500; %hz
N = length(ECG_data);
duration_in_seconds = N/fs;
duration_in_minutes = duration_in_seconds/60;
BPM = beat_count/duration_in_minutes;
disp(BPM);

fileID = fopen('wyjscie.txt','w');
fprintf(fileID, 'Tetno: %.2f uderzen na minute\n\n',BPM');

fprintf(fileID, 'Sygnaly: R ilosc: %.f\n\n',length(locs_Rwave)');

print_result(fileID,locs_Rwave);
fprintf(fileID, '\nSygnaly: S ilosc: %.f\n\n',length(locs_Swave)');

print_result(fileID,locs_Qwave);
fprintf(fileID, '\nSygnaly: Q ilosc: %.f\n\n',length(locs_Qwave)');

print_result(fileID,locs_Swave);
fprintf(fileID, '\nSygnaly P i T ilosc: %.f\n\n',length(locs_Twave)');
print_result(fileID,locs_Twave);




function f = print_result(fileID,data)
	for k =1:length(data)
	fprintf(fileID, '%.2f\n',data(k));
    end
end







