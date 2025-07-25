import { useState, useEffect } from 'react';
import { Container, Box, Typography, Paper, Button, CircularProgress, Stack } from '@mui/material';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker';
import { DataGrid } from '@mui/x-data-grid';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import utc from 'dayjs/plugin/utc';
import { Version } from './Version.jsx';
import './App.css'

// Extend dayjs with needed plugins
dayjs.extend(relativeTime);
dayjs.extend(utc);

function App() {

  // We set the default since value to 8pm yesterday
  const yesterday8pm = dayjs().subtract(1, 'day').hour(20).minute(0).second(0).millisecond(0);
  const [since, setSince] = useState(yesterday8pm);
  const [statusData, setStatusData] = useState();
  const [fetching, setFetching] = useState(false);
  const [lastFetch, setLastFetch] = useState();
  const [lastFetchDisplay, setLastFetchDisplay] = useState();

  const fetchData = (since) => {
    setFetching(true);
    const baseUrl = '/api/v1';
    const statusUrl = since ? `${baseUrl}/status?_since=${since.toISOString()}` : `${baseUrl}/status`;
    fetch(statusUrl).then((result) => {
      return result.json();
    }).then((json) => {
      setStatusData(json);
      setLastFetch(dayjs());
    }).catch((e) => {
      console.log(e);
    }).finally(() => {
      setFetching(false);
    });
  }

  // Fetch status JSON on first render and whenever "since" changes
  useEffect(() => {
    fetchData(since);
  }, [since]);

  // Update the display of how long ago the last fetch happened every second
  useEffect(() => {
    const intervalId = setInterval(() => {
      setLastFetchDisplay(lastFetch ? dayjs(lastFetch).fromNow() : '');
    }, 1000); // Update every 1 second
    return () => clearInterval(intervalId); // Cleanup on unmount
  }, [lastFetch]);

  // Instead of showing dates and times show "N minutes ago" or similar
  const relativeDate = (date) => {
    // If no queued messages date comes back as 0001-01-01
    if (!dayjs(date).isSame('0001-01-01', 'day')) {
      // Time comes from server in UTC but without the Z indicating such, we append it
      return dayjs.utc(`${date}Z`).fromNow();
    }
  }

  const columns = [
    { field: 'messageCount', headerName: '# Messages', type: 'number', valueGetter: (value, row) => row.processedCount + row.queuedCount, width: 130 },
    { field: 'receivedOneHour', headerName: 'Received last 1hr', type: 'number', valueGetter: (value, row) => row.processedCountOneHour + row.queuedCountOneHour, width: 140 },
    { field: 'receivedFiveMinutes', headerName: 'Received last 5min', type: 'number', valueGetter: (value, row) => row.processedCountFiveMinutes + row.queuedCountFiveMinutes, width: 140 },
    { field: 'processedCount', headerName: '# Processed', type: 'number', width: 130 },
    { field: 'processedCountOneHour', headerName: 'Processed last 1hr', type: 'number', width: 140 },
    { field: 'processedCountFiveMinutes', headerName: 'Processed last 5min', type: 'number', width: 150 },
    { field: 'queuedCount', headerName: '# Queued', type: 'number', width: 110 },
    { field: 'oldest', headerName: 'Oldest Queued', sortable: false, valueGetter: (value, row) => relativeDate(row.oldestQueued), width: 140 },
    { field: 'newest', headerName: 'Newest Queued', sortable: false, valueGetter: (value, row) => relativeDate(row.newestQueued), width: 140 },
    { field: 'latestProcessed', headerName: 'LatestProcessed', sortable: false, valueGetter: (value, row) => relativeDate(row.latestProcessed), width: 140 },
  ];

  const sourceColumns = [
    { field: 'source', headerName: 'Source', width: 100 },
  ].concat(columns)

  const eventTypeColumns = [
    { field: 'eventType', headerName: 'Event Type', width: 120 },
  ].concat(columns)

  const jurisdictionColumns = [
    { field: 'jurisdictionId', headerName: 'Jurisdiction', width: 121 },
  ].concat(columns)

  const allJurisdictionRows = statusData ? [{ jurisdictionId: 'All', ...statusData }] : [];
  const sourceRows = statusData?.sourceResults || [];
  const eventTypeRows = statusData?.eventTypeResults || [];
  const jurisdictionRows = statusData?.jurisdictionResults || [];

  return (
    <Container maxWidth={false}>

      <Box sx={{ width: '100%', mb: 2 }}>
        <Button disabled={fetching} variant="contained" sx={{ float: 'right' }} onClick={() => fetchData(since)}>Refresh {fetching && <CircularProgress size={15} sx={{ ml: 1 }}/>}</Button>
        <Typography variant="h6" sx={{ float: 'right', mr: 2, mt: 0.5, fontSize: '1.1em' }}>Last refreshed {lastFetchDisplay}</Typography>
        <Typography variant="h5">FHIR API Status {statusData && statusData.apiEnvironment && `(${statusData.apiEnvironment})`}</Typography>
        <Stack direction="row" alignItems="center" spacing={2} sx={{ mt: 2 }}>
          <Typography variant="h5" >Since</Typography>
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            <DateTimePicker label="Fetch data since date picker" value={since} onChange={(newDateTime) => setSince(newDateTime)} />
          </LocalizationProvider>
        </Stack>
      </Box>
      
      <DataGrid
        sx={{ width: '100%', mb: 3 }}
        rows={allJurisdictionRows}
        getRowId={(row) => row.jurisdictionId }
        columns={jurisdictionColumns}
        initialState={{
          sorting: {
            sortModel: [{ field: 'jurisdictionId', sort: 'asc' }],
          },
        }}
      />

      <Box sx={{ width: '100%', mb: 2 }}>
        <Typography variant="h5">Status By Source</Typography>
      </Box>

      <DataGrid
        sx={{ width: '100%', mb: 3 }}
        rows={sourceRows}
        getRowId={(row) => row.source }
        columns={sourceColumns}
        initialState={{
          sorting: {
            sortModel: [{ field: 'source', sort: 'asc' }],
          },
        }}
      />

      <Box sx={{ width: '100%', mb: 2 }}>
        <Typography variant="h5">Status By Event Type</Typography>
      </Box>

      <DataGrid
        sx={{ width: '100%', mb: 3 }}
        rows={eventTypeRows}
        getRowId={(row) => row.eventType }
        columns={eventTypeColumns}
        initialState={{
          sorting: {
            sortModel: [{ field: 'eventType', sort: 'asc' }],
          },
        }}
      />

      <Box sx={{ width: '100%', mb: 2 }}>
        <Typography variant="h5">Status By Jurisdiction</Typography>
      </Box>

      <DataGrid
        sx={{ width: '100%' }}
        rows={jurisdictionRows}
        getRowId={(row) => row.jurisdictionId }
        columns={jurisdictionColumns}
        initialState={{
          sorting: {
            sortModel: [{ field: 'jurisdictionId', sort: 'asc' }],
          },
        }}
      />

      <Version />
    </Container>
  )
}

export default App
