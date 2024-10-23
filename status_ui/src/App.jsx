import { useState, useEffect } from 'react';
import { Container, Box, Typography, Paper, Button, CircularProgress } from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import moment from 'moment';
import './App.css'

function App() {

  const [statusData, setStatusData] = useState();
  const [fetching, setFetching] = useState(false);
  const [lastFetch, setLastFetch] = useState();
  const [lastFetchDisplay, setLastFetchDisplay] = useState();

  const fetchData = () => {
    setFetching(true);
    fetch('/status').then((result) => {
      return result.json();
    }).then((json) => {
      setStatusData(json);
      setLastFetch(moment());
    }).catch((e) => {
      console.log(e);
    }).finally(() => {
      setFetching(false);
    });
  }

  // Fetch status JSON on first render
  useEffect(() => {
    fetchData();
  }, []);

  // Update the display of how long ago the last fetch happened every second
  useEffect(() => {
    const intervalId = setInterval(() => {
      setLastFetchDisplay(moment(lastFetch).fromNow());
    }, 1000); // Update every 1 second
    return () => clearInterval(intervalId); // Cleanup on unmount
  }, [lastFetch]);

  // Instead of showing dates and times show "N minutes ago" or similar
  const relativeDate = (date) => {
    // If no queued messages date comes back as 0001-01-01
    if (!moment(date).isSame('0001-01-01', 'day')) {
      // Time comes from server in UTC but without the Z indicating such, we append it
      return moment(`${date}Z`).fromNow();
    }
  }

  const columns = [
    { field: 'jurisdictionId', headerName: 'Jurisdiction', width: 121 },
    { field: 'messageCount', headerName: '# Messages', type: 'number', valueGetter: (value, row) => row.processedCount + row.queuedCount, width: 130 },
    { field: 'receivedOneHour', headerName: 'Received last 1hr', type: 'number', valueGetter: (value, row) => row.processedCountOneHour + row.queuedCountOneHour, width: 140 },
    { field: 'receivedFiveMinutes', headerName: 'Received last 5min', type: 'number', valueGetter: (value, row) => row.processedCountFiveMinutes + row.queuedCountFiveMinutes, width: 140 },
    { field: 'processedCount', headerName: '# Processed', type: 'number', width: 130 },
    { field: 'processedCountOneHour', headerName: 'Processed last 1hr', type: 'number', width: 140 },
    { field: 'processedCountFiveMinutes', headerName: 'Processed last 5min', type: 'number', width: 150 },
    { field: 'queuedCount', headerName: '# Queued', type: 'number', width: 110 },
    { field: 'oldest', headerName: 'Oldest Queued', sortable: false, valueGetter: (value, row) => relativeDate(row.oldestQueued), width: 140 },
    { field: 'newest', headerName: 'Newest Queued', sortable: false, valueGetter: (value, row) => relativeDate(row.newestQueued), width: 140 },
  ];

  const allJurisdictionRows = statusData ? [{ jurisdictionId: 'All', ...statusData }] : [];
  const jurisdictionRows = statusData?.jurisdictionResults || [];

  return (
    <Container maxWidth={false}>

      <Box sx={{ width: '100%', mb: 2 }}>
        <Button disabled={fetching} variant="contained" sx={{ float: 'right' }} onClick={() => fetchData()}>Refresh {fetching && <CircularProgress size={15} sx={{ ml: 1 }}/>}</Button>
        <Typography variant="h6" sx={{ float: 'right', mr: 2, mt: 0.5, fontSize: '1.1em' }}>Last refreshed {lastFetchDisplay}</Typography>
        <Typography variant="h4">FHIR API Status</Typography>
      </Box>
      
      <DataGrid
        sx={{ width: '100%', mb: 3 }}
        rows={allJurisdictionRows}
        getRowId={(row) => row.jurisdictionId }
        columns={columns}
        initialState={{
          sorting: {
            sortModel: [{ field: 'jurisdictionId', sort: 'asc' }],
          },
        }}
      />

      <Box sx={{ width: '100%', mb: 2 }}>
        <Typography variant="h4">Status By Jurisdiction</Typography>
      </Box>

      <DataGrid
        sx={{ width: '100%' }}
        rows={jurisdictionRows}
        getRowId={(row) => row.jurisdictionId }
        columns={columns}
        initialState={{
          sorting: {
            sortModel: [{ field: 'jurisdictionId', sort: 'asc' }],
          },
        }}
      />

    </Container>
  )
}

export default App
